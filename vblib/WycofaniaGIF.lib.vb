


Imports System.Net
Imports System.Net.Http.Headers
Imports System.Runtime.CompilerServices

Imports pkar
Imports pkar.DotNetExtensions

Public Class WycofaniaGIF

    Private Shared _wycofania As BaseList(Of ImportowanaDecyzja)

    Public Sub New(cachedir As String)
        _wycofania = New BaseList(Of ImportowanaDecyzja)(cachedir, "gifdec.json")
    End Sub

    Public Sub LoadCache()
        _wycofania.Load()
    End Sub

    'Public Sub ImportFull()
    '    ' https://rdg.ezdrowie.gov.pl/Decision/DownloadPublicXml - ale jest redirect na tym do konkretnego dnia, RejestrDecyzjiGIF20250205.xml
    'End Sub

    ''' <summary>
    ''' wczytuje z sieci decyzje od ostatniej wczytanej - wymaga więc sieci :) . Zapisuje zmieniony cache.
    ''' </summary>
    ''' <param name="maxCount">Ile maksymalnie decyzji może wczytać</param>
    Public Async Function ImportNewDecyzje(maxCount As Integer) As Task(Of String)
        ' https://rdg.ezdrowie.gov.pl/ - na tej stronie jest ostatnie 25 decyzji, w tym wycofań

        Dim decNum As Integer

        If _wycofania.Count > 0 Then decNum = _wycofania.Max(Of Integer)(Function(x) x.nr)
        decNum += 1
        If decNum < 4451 Then decNum = 4451
        ' 2025: od 4567
        ' 2024: od 4451, 116 decyzji
        ' 2023: od 4326, 125 decyzji

        Dim bAdded As Boolean

        Dim retMsg As String = ""

        Do
            Dim decyzja As ImportowanaDecyzja = Await DownloadDecyzja(decNum)

            If decyzja Is Nothing Then
                Debug.WriteLine($"Decyzji {decNum} już nie ma, zapisuję plik")
                If bAdded Then _wycofania.Save()
                Return retMsg
            End If

            Debug.WriteLine($"Decyzja {decNum}: {decyzja.decyzja} dla {decyzja.nazwa}, guard={maxCount}")

            If decyzja.data IsNot Nothing Then
                _wycofania.Add(decyzja)
                bAdded = True

                retMsg &= $"{decyzja.decyzja} dla '{decyzja.nazwa}'" & vbCrLf

            End If

            decNum += 1

            maxCount -= 1
            If maxCount < 0 Then
                Debug.WriteLine($"Guard zareagował, zapisuję plik")
                If bAdded Then _wycofania.Save()
                Return retMsg
            End If

            Await Task.Delay(100)
        Loop

        'Dim cosik = Await ImportDecyzja("4577")
        'Dim cosik1 = Await ImportDecyzja("4588")    ' tego jeszcze nie ma

        Return retMsg

    End Function


    Public Function CheckEAN(ean As String) As GIFstatus

        If ean.NotStartsWith("0") Then ean = "0" & ean

        For iLp As Integer = _wycofania.Count - 1 To 0 Step -1
            Dim decyzja As ImportowanaDecyzja = _wycofania.Item(iLp)
            If decyzja.gtin = ean Then
                If decyzja.decyzja.ContainsCI("wycofani") Then Return GIFstatus.Wycofany
                If decyzja.decyzja.ContainsCI("Ponowne dopuszczenie") Then Return GIFstatus.Wznowiony
                If decyzja.decyzja.ContainsCI("wstrzyman") Then Return GIFstatus.Wstrzymany
                Return GIFstatus.Unrecognized
            End If
        Next

        Return GIFstatus.NoInfo
    End Function

    Public Function CheckEAN(lek As JednoPudelko) As GIFstatus

        Dim status As GIFstatus = CheckEAN(lek.sBarcode)
        If status <> GIFstatus.NoInfo Then Return status

        If String.IsNullOrWhiteSpace(lek.sOpakowania) Then Return status
        ' 05909990062928
        ' 0590999.......
        For Each kod As System.Text.RegularExpressions.Match In Text.RegularExpressions.Regex.Matches(lek.sOpakowania, "0590999[0-9][0-9][0-9][0-9][0-9][0-9][0-9]")
            status = CheckEAN(kod.Value)
            If status <> GIFstatus.NoInfo Then Return status
        Next

        Return GIFstatus.NoInfo

    End Function

    Public Function GetDecyzjaUri(ean As String) As Uri

        If ean.NotStartsWith("0") Then ean = "0" & ean

        For iLp As Integer = _wycofania.Count - 1 To 0 Step -1
            Dim decyzja As ImportowanaDecyzja = _wycofania.Item(iLp)
            If decyzja.gtin = ean Then
                Return New Uri($"https://rdg.ezdrowie.gov.pl/Decision/Decision?id={decyzja.nr}")
            End If
        Next

        Return Nothing
    End Function


    Public Function GetDecyzjaUri(lek As JednoPudelko) As Uri

        Dim linek As Uri = GetDecyzjaUri(lek.sBarcode)
        If linek IsNot Nothing Then Return linek

        If String.IsNullOrWhiteSpace(lek.sOpakowania) Then Return Nothing

        ' 05909990062928
        ' 0590999.......
        For Each kod As System.Text.RegularExpressions.Match In Text.RegularExpressions.Regex.Matches(lek.sOpakowania, "0590999[0-9][0-9][0-9][0-9][0-9][0-9][0-9]")
            linek = GetDecyzjaUri(kod.Value)
            If linek IsNot Nothing Then Return linek
        Next

        Return Nothing

    End Function

#Region "prywatności"


    Private Shared _ohttp As New Net.Http.HttpClient

    ''' <summary>
    ''' Ściąga decyzję o podanym numerze
    ''' </summary>
    ''' <param name="id">Nr decyzji do ściągnięcia</param>
    ''' <returns>NULL gdy błąd; lub ImportowanaDecyzja z wstawionym id, gdy reszta pól NULL, to decyzja pusta</returns>
    Private Async Function DownloadDecyzja(id As Integer) As Task(Of ImportowanaDecyzja)
        Dim sUri As String = "https://rdg.ezdrowie.gov.pl/Decision/Decision?id=" & id

        Try
            Dim sPage As String = Await _ohttp.GetStringAsync(sUri)

            Return ParseDecyzja(id, sPage)
        Catch ex As Exception
            Return Nothing
        End Try

    End Function


    ''' <summary>
    ''' Importuje decyzję z pliku HTML
    ''' </summary>
    ''' <param name="id">tylko do wstawienia do obiektu</param>
    ''' <param name="html">wczytana strona html</param>
    ''' <returns>NULL gdy błąd; lub ImportowanaDecyzja z wstawionym id, gdy reszta pól NULL, to decyzja pusta</returns>
    Private Function ParseDecyzja(id As Integer, html As String) As ImportowanaDecyzja
        ' ale błąd powinno się wychwycić wcześniej, bo httpcode=500
        If html.Contains("Odwołanie do obiektu nie zostało ustawione na wystąpienie obiektu") Then Return Nothing

        Dim oNew As New ImportowanaDecyzja
        oNew.nr = id

        ' tego typu decyzji nie pamiętamy - one i tak nie mają EAN
        If html.ContainsCI("Decyzja o zakazie wprowadzenia") Then Return oNew

        oNew.data = ExtractRowData(html, "Data decyzji")
        oNew.decyzja = html.SubstringBetweenExclusive("<title>", "</title>").Replace("RDG", "").Replace("-", "").Trim
        oNew.nazwa = ExtractRowData(html, "Nazwa")
        oNew.moc = ExtractRowData(html, "Moc")
        oNew.postac = ExtractRowData(html, "Postać")

        oNew.gtin = ExtractRowData(html, "GTIN")
        oNew.wielkosc = ExtractRowData(html, "Wielkość opakowania")

        oNew.serie = ExtractSerie(html)

        Return oNew

    End Function

    Private Function ExtractRowData(html As String, query As String) As String
        Dim iInd As Integer = html.IndexOf(">" & query & "<")
        If iInd < 0 Then Return Nothing

        Dim ret As String = html.Substring(iInd + query.Length + 1)

        iInd = ret.IndexOfFirst("row details-row", "<hr")
        If iInd > 10 Then ret = ret.Substring(0, iInd)

        ret = ret.StripHtmlTags
        If ret.StartsWith("*") Then ret = ret.Substring(1)
        Return ret.Trim
    End Function

    Private Function ExtractSerie(html As String) As List(Of ImportowanaDecyzja_Seria)

        ' tak sobie zaznaczamy wszystkie serie (tak, wiem, i tak taki jest default)
        If html.ContainsCI("Wszystkie Serie") Then Return Nothing

        Dim iInd As Integer = html.IndexOf("details-row-series")
        If iInd < 1 Then Return Nothing

        Dim ret As New List(Of ImportowanaDecyzja_Seria)

        Dim temp As String = html


        Do
            iInd = temp.IndexOf(">Numer serii<")
            If iInd < 1 Then Exit Do
            temp = temp.Substring(iInd)

            Dim oNew As New ImportowanaDecyzja_Seria
            oNew.nr = ExtractRowData(temp, "Numer serii")
            oNew.datawazn = ExtractRowData(temp, "Data ważności")
            ret.Add(oNew)

            temp = temp.Substring(10) ' tak żeby nie było numer serii
        Loop

        Return ret
    End Function


    ' dzis: 5 II
    ' wycofanie Septogard Plus, 31 I
    ' https://rdg.ezdrowie.gov.pl/Decision/Decision?id=4577


    Protected Class ImportowanaDecyzja
        Inherits BaseStruct

        Public Property nr As Integer
        Public Property data As String
        Public Property decyzja As String ' ' Zakaz wprowadzenia, Wycofanie z obrotu, Wstrzymanie w obrocie, Ponowne dopuszczenie do obrotu
        Public Property nazwa As String
        Public Property moc As String
        Public Property postac As String
        Public Property gtin As String
        Public Property wielkosc As String
        Public Property serie As List(Of ImportowanaDecyzja_Seria)


    End Class

    Public Class ImportowanaDecyzja_Seria
        Public Property nr As String
        Public Property datawazn As String
    End Class

#End Region


End Class

Public Enum GIFstatus
    NoInfo
    Wstrzymany
    Wznowiony
    Wycofany
    Unrecognized
End Enum

