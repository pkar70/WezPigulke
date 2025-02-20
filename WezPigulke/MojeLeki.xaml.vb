
' lista leków które już znamy (roaming) - wedle typu, potem nazwy (czynnej?)
' przy każdym do wyboru typ leku (stały, etc.)
' context: zmiana typu, pokaż ulotkę, pokaż charakterystykę, 

' z dolnego menu: ściągnij ulotkę i charakterystykę (do localcache albo local, nie roaming!)
' szukaj interakcji (wedle nazwy czynnej)

' refresh danych? jakby doszly nowe EANy na przyklad?

Imports pkar.UI.Extensions
Imports pkar.UWP ' dla OneDrive
Imports pkar.DotNetExtensions
Imports vblib
Imports pkar

Public NotInheritable Class MojeLeki
    Inherits Page

    Private moHttp As Windows.Web.Http.HttpClient

    Private _sortMode As LekiSortMode = LekiSortMode.typNazwa

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        PokazListe()
    End Sub

#Region "sortowanie"

    ' ewentualnie CommandParameter, tylko wtedy nie ma Enum i sam muszę pilnować integerów; albo z oFE.name

    Protected Enum LekiSortMode
        typNazwa
        substNazwa
        nazwa
        ATC
    End Enum

    Private Sub uiSortTypNazwa_Click(sender As Object, e As RoutedEventArgs)
        PokazListe(LekiSortMode.typNazwa)
    End Sub

    Private Sub uiSortSubstNazwa_Click(sender As Object, e As RoutedEventArgs)
        PokazListe(LekiSortMode.substNazwa)
    End Sub

    Private Sub uiSortNazwa_Click(sender As Object, e As RoutedEventArgs)
        PokazListe(LekiSortMode.nazwa)
    End Sub
    Private Sub uiSortATC_Click(sender As Object, e As RoutedEventArgs)
        PokazListe(LekiSortMode.ATC)
    End Sub

    Private Sub PokazListe()
        PokazListe(_sortMode)
    End Sub

    Private Sub PokazListe(sortMode As LekiSortMode)

        Dim lista As IEnumerable(Of vblib.JednoPudelko) = vblib.globalsy.glZnanePudelka

        Dim filtr As String = uiFilter.Text
        If Not String.IsNullOrWhiteSpace(filtr) Then
            lista = lista.Where(Function(x) x.sNazwa.ContainsCI(filtr) OrElse x.sNazwaCzynna.ContainsCI(filtr))
        End If

        Select Case sortMode
            Case LekiSortMode.nazwa
                uiList.ItemsSource = From c In lista Order By c.sNazwa
            Case LekiSortMode.substNazwa
                uiList.ItemsSource = From c In lista Order By c.sNazwaCzynna, c.sNazwa
            Case LekiSortMode.ATC
                uiList.ItemsSource = From c In lista Order By c.sKodATC
            Case Else
                uiList.ItemsSource = From c In lista Order By c.iTypLeku Descending, c.sNazwa
        End Select
    End Sub
#End Region

    Private Function Sender2Pudelko(sender As Object) As vblib.JednoPudelko
        Dim oFE As FrameworkElement
        oFE = TryCast(sender, FrameworkElement)
        If oFE Is Nothing Then Return Nothing
        Return TryCast(oFE.DataContext, vblib.JednoPudelko)
    End Function

    Private Sub uiShowDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As vblib.JednoPudelko = Sender2Pudelko(sender)
        If oItem Is Nothing Then Return
        Me.Frame.Navigate(GetType(DaneLeku), oItem.sBarcode)
    End Sub

    Private Async Sub uiShowUlotka_Click(sender As Object, e As RoutedEventArgs)
        ' https://rejestry.ezdrowie.gov.pl/medicinal-products/30714/leaflet
        Dim errmsg As String = Await DaneLeku.PokazPDF(Sender2Pudelko(sender), False)
        If String.IsNullOrWhiteSpace(errmsg) Then Return
        Me.MsgBox(errmsg)
    End Sub

    Private Async Sub uiShowCharakt_Click(sender As Object, e As RoutedEventArgs)
        ' https://rejestry.ezdrowie.gov.pl/medicinal-products/30714/characteristic
        Dim errmsg As String = Await DaneLeku.PokazPDF(Sender2Pudelko(sender), True)
        If String.IsNullOrWhiteSpace(errmsg) Then Return
        Me.MsgBox(errmsg)
    End Sub


    Private _isChanged As Boolean = False

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        ' zapisanie, mógłby tylko po zmianach, tylko jak to wychwycić?
        If _isChanged Then
            Debug.WriteLine("chyba mam zmienione, i powinienem zapisac")
            vblib.globalsy.glZnanePudelka.Save()
        End If
    End Sub

    Private Sub UCtypLeku_ValueChanged(sender As Object, e As RoutedEventArgs)
        _isChanged = True
    End Sub


    Private Async Function Interakcje_SciagnijDaneSubstancji(oLek As vblib.JednoPudelko) As Task(Of vblib.JednaSubstancja)
        ' zwraca sId substancji

        Dim oSubst As vblib.JednaSubstancja = Await oLek.ZnajdzSubstancje
        If oSubst IsNot Nothing Then Return oSubst

        oSubst = New vblib.JednaSubstancja
        oSubst.sNazwa = oLek.sNazwaCzynna

        If moHttp Is Nothing Then
            moHttp = New Windows.Web.Http.HttpClient
        End If

        Me.ProgRingShow(True)

        Dim sBaseUri As String = "https://ktomalek.pl/l/interakcje/"
        Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing
        Dim sResp As String = ""
        oResp = Await moHttp.GetAsync(New Uri(sBaseUri & "pomiedzy-lekami"))
        sResp = Await oResp.Content.ReadAsStringAsync
        Await Task.Delay(100)   ' poczekajmy

        Dim sUri As String
        sUri = sBaseUri & "szukajBrandowISubstancjiAjax?searchInput=" & oLek.sNazwaCzynna

        oResp = Await moHttp.GetAsync(New Uri(sUri))
        sResp = Await oResp.Content.ReadAsStringAsync

        Dim iInd As Integer
        iInd = sResp.IndexOf("Znalezione substancje czynne")
        If iInd < 1 Then
            Me.ProgRingShow(False)
            Return Nothing
        End If

        sResp = sResp.Substring(iInd)
        ' brand.dodajDoSesji(1461, &apos;RAMIPRILUM&apos;, &apos;SUBSTANCJA&apos;
        ' zamienic na
        ' https://ktomalek.pl/l/interakcje/dodajDoSesjiAjax?id=1461&nazwa=RAMIPRILUM&typ=SUBSTANCJA
        iInd = sResp.IndexOf("dodajDoSesji")
        If iInd > 0 Then
            sResp = sResp.Substring(iInd + 13)
            iInd = sResp.IndexOf(",")
            If iInd > 0 Then oSubst.sId = sResp.Substring(0, iInd)
        End If
        If iInd < 1 Then
            Me.ProgRingShow(False)
            Return Nothing
        End If

        'sResp = sResp.Substring(iInd)
        'iInd = sResp.IndexOf(";")
        'If iInd < 1 Then Return
        'sResp = sResp.Substring(iInd + 1)
        'iInd = sResp.IndexOf("&")
        'If iInd < 1 Then Return
        'sResp = sResp.Substring(0, iInd)
        'oItem.sIdSubstancji = oItem.sIdSubstancji & "&nazwa=" & sResp

        vblib.globalsy.ZnaneSubstancjeAddChange(oSubst)
        vblib.globalsy.glZnaneSubstancje.Save() ' App.ZnaneSubstancjeSave     ' zapisz, jakby potem był błąd - ten krok będzie można pominąć

        Me.ProgRingShow(False)
        Return oSubst

    End Function


    Private Function InterakcjeLekow_SprawdzZaznaczenia() As Integer
        'Dim iLekiCnt As Integer = 0
        'For Each oLek As JednoPudelko In From c In App.glZnanePudelka Where c.bIncludeInteraction
        '    iLekiCnt += 1
        'Next
        'Return iLekiCnt
        Return (From c In vblib.globalsy.glZnanePudelka Where c.bIncludeInteraction).Count
    End Function

    Private Async Function InterakcjeLekow_ZnamSubstancje() As Task(Of Boolean)
        For Each oLek As vblib.JednoPudelko In From c In vblib.globalsy.glZnanePudelka Where c.bIncludeInteraction
            Dim bFound As Boolean = False
            For Each oSubst As vblib.JednaSubstancja In vblib.globalsy.glZnaneSubstancje
                If oSubst.sNazwa.ToLower = oLek.sNazwaCzynna.ToLower Then
                    bFound = True
                    Exit For
                End If
            Next
            If Not bFound Then
                If Await Interakcje_SciagnijDaneSubstancji(oLek) Is Nothing Then Return False
            End If
        Next
        Return True
    End Function

    Private Async Sub uiInterakcje_Click(sender As Object, e As RoutedEventArgs)
        ' dla kazdego ktory jest bIncludeInteraction
        ' https://ktomalek.pl/l/interakcje/lekow-z-zywnoscia
        ' GET https://ktomalek.pl/l/interakcje/dodajDoSesjiAjax?id=1461&nazwa=RAMIPRILUM&typ=SUBSTANCJA
        ' xhtmlrequest
        ' zwraca: cos jakby url, ktory potem jest aktualny
        ' Nie posiadamy informacji wskazujących, aby wybrana substancja wchodziła w interakcje z żywnością

        ' https://ktomalek.pl/l/interakcje/pomiedzy-lekami

        If InterakcjeLekow_SprawdzZaznaczenia() < 2 Then
            Me.MsgBox("Interakcje między lekami wymagają zaznaczenia przynajmniej dwu leków")
            Return
        End If

        Me.ProgRingShow(True)
        If Not Await InterakcjeLekow_ZnamSubstancje() Then
            Me.MsgBox("Nie znam substancji do niektórych leków!")
            ' *TODO* tylko ignorowanie, bo:
            ' lek bez substancji można zignorować (albo dodać jako lek, nie jako substancje)
            Me.ProgRingShow(False)
            Return
        End If

        'If moHttp Is Nothing Then
        '    moHttp = New Windows.Web.Http.HttpClient
        'End If

        'Dim sBaseUri As String = "https://ktomalek.pl/l/interakcje/"
        'Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing
        'Dim sResp As String = ""
        'oResp = Await moHttp.GetAsync(New Uri(sBaseUri & "pomiedzy-lekami"))
        'sResp = Await oResp.Content.ReadAsStringAsync
        'Await Task.Delay(100)   ' poczekajmy

        'For Each oLek In From c In App.glZnanePudelka Where c.bIncludeInteraction
        '    For Each oSubst As JednaSubstancja In App.glZnaneSubstancje
        '        If oSubst.sNazwa.ToLower = oLek.sNazwaCzynna.ToLower Then
        '            ' dodaj do interakcji
        '            Dim sUri As String = sBaseUri & "pomiedzy-lekami/a/su-" & oSubst.sId
        '            oResp = Await moHttp.GetAsync(New Uri(sUri))
        '            sResp = Await oResp.Content.ReadAsStringAsync
        '            Await Task.Delay(100)   ' poczekajmy
        '            Exit For
        '        End If
        '    Next
        'Next

        '' sResp teoretycznie ma dane o interakcjach
        'Dim iInd As Integer = 0

        Me.ProgRingShow(False)
        Me.MsgBox("Jeszcze nie umiem")

    End Sub

    Private Sub uiPokazDecyzje_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As vblib.JednoPudelko = Sender2Pudelko(sender)
        If oItem Is Nothing Then Return

        If vblib.globalsy.glWycofaniaGIF Is Nothing Then
            vblib.globalsy.glWycofaniaGIF = New vblib.WycofaniaGIF(Windows.Storage.ApplicationData.Current.RoamingFolder.Path)
            vblib.globalsy.glWycofaniaGIF.LoadCache()
        End If
        ' nie aktualizujemy, bo przecież mamy zapisane że jest decyzja - czyli już ją znamy

        Dim linek As Uri = vblib.globalsy.glWycofaniaGIF.GetDecyzjaUri(oItem)
        If linek IsNot Nothing Then linek.OpenBrowser
    End Sub

    Private Async Function PokazInterakcje(sender As Object, bJedzenie As Boolean) As Task
        ' lek z alkoholem/zywnoscia , nie pomiedzy lekami

        Dim oItem As vblib.JednoPudelko = Sender2Pudelko(sender)
        If oItem Is Nothing Then Return

        Dim oSubst As vblib.JednaSubstancja = Await oItem.ZnajdzSubstancje
        If oSubst Is Nothing Then
            Me.MsgBox("Nie znalazłem substancji czynnej?")
            Return
        End If

        Dim sMsg As String = ""
        sMsg = If(bJedzenie, oSubst.sInterJedz, oSubst.sInterAlk)
        If sMsg.Length > 2 Then
            Me.MsgBox(sMsg)
            Return
        End If

        'If Not bFound Then oSubst = New JednaSubstancja
        'oSubst.sNazwa = oItem.sNazwaCzynna

        Dim sLink As String = If(bJedzenie, "lekow-z-zywnoscia", "lekow-z-alkoholem")

        If moHttp Is Nothing Then
            moHttp = New Windows.Web.Http.HttpClient
        End If

        Me.ProgRingShow(True)

        Dim sBaseUri As String = "https://ktomalek.pl/l/interakcje/"
        Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing
        Dim sResp As String = ""
        oResp = Await moHttp.GetAsync(New Uri(sBaseUri & sLink))
        sResp = Await oResp.Content.ReadAsStringAsync
        Await Task.Delay(100)   ' poczekajmy

        Dim sUri As String = sBaseUri & sLink & "/a/br-" & oSubst.sId ' & sSubstId

        'sUri = sBaseUri & "dodajDoSesjiAjax?id=" & oItem.sIdSubstancji & "&typ=SUBSTANCJA"
        oResp = Await moHttp.GetAsync(New Uri(sUri))
        sResp = Await oResp.Content.ReadAsStringAsync

        ' zdaje sie ze jak tak jest, to znaczy ze nie ma interakcji

        If sResp.Contains("message-valid") AndAlso sResp.Contains("Nie posiadamy informacji wskazujących") Then
            If bJedzenie Then
                oSubst.sInterJedz = "Brak interakcji"
            Else
                oSubst.sInterAlk = "Brak interakcji"
            End If
            vblib.globalsy.ZnaneSubstancjeAddChange(oSubst)
            vblib.globalsy.glZnaneSubstancje.Save()
            'Dim oTask As Task = App.ZnaneSubstancjeSave
            Me.MsgBox("Brak interakcji")
            'Await oTask ' wykorzystujemy czas dialogboxa
            Me.ProgRingShow(False)
            Return
        End If

        ' a teraz moze jest interakcja
        ' box panel
        Dim iInd As Integer
        If sResp.Contains("<article") Then
            iInd = sResp.IndexOf("<article")
            sResp = sResp.Substring(iInd)
            iInd = sResp.IndexOf("</article")
            sResp = sResp.Substring(0, iInd)

            iInd = sResp.IndexOf("<h3")
            sResp = sResp.Substring(iInd)
            iInd = sResp.IndexOf("</h3")
            Dim sInterakcja As String = sResp.Substring(0, iInd).StripHtmlTags ') ' istotna, etc.

            iInd = sResp.IndexOf("<p", iInd)
            sResp = sResp.Substring(iInd)

            sInterakcja = sInterakcja & vbCrLf & sResp.StripHtmlTags

            If bJedzenie Then
                oSubst.sInterJedz = sInterakcja
            Else
                oSubst.sInterAlk = sInterakcja
            End If
            vblib.globalsy.ZnaneSubstancjeAddChange(oSubst)
            'Dim oTask As Task = App.ZnaneSubstancjeSave
            Me.MsgBox(sInterakcja)
            'Await oTask ' wykorzystujemy czas dialogboxa
            vblib.globalsy.glZnaneSubstancje.Save()
        Else
            Me.MsgBox("Nie rozumiem odpowiedzi serwera")
        End If

        Me.ProgRingShow(False)


        ' https://ktomalek.pl/l/interakcje/szukajBrandowISubstancjiAjax?searchInput=rami

    End Function

    Private Sub uiInterJedz_Click(sender As Object, e As RoutedEventArgs)
        ' ewentualnie jakiś cache do tego, typu substancjaczynna i jej interakcje
        ' albo lek i jego interakcje, jak nie ma substancji czynnej (bądź: wiele, jak broncho)
        ' https://ktomalek.pl/l/interakcje/lekow-z-zywnoscia
        PokazInterakcje(sender, True)
    End Sub

    Private Sub uiInterAlk_Click(sender As Object, e As RoutedEventArgs)
        ' https://ktomalek.pl/l/interakcje/lekow-z-alkoholem
        PokazInterakcje(sender, False)
    End Sub

#Region "onedrive"
    Dim _Odrive As New OneDriveSync({"pudelka.xml", "substancje.xml", "zestawy.xml", "pudelka.json", "substancje.json", "zestawy.json", "gifdec.json"}, Windows.Storage.ApplicationData.Current.RoamingFolder)
    Private Async Sub uiODsync_Click(sender As Object, e As RoutedEventArgs)

        ' inicjalizacja settings - tu potrzeba (do Nugeta OneDrive), ale będzie zbędna
        Dim bErr As String = ""
        'Try
        '    InitSettings("", False, "ala", Nothing, Nothing, Windows.Storage.ApplicationData.Current.LocalFolder.Path, Nothing, False, Nothing)
        'Catch ex As Exception
        '    bErr = ex.Message
        'End Try

        'If bErr <> "" Then
        '    Me.MsgBox(bErr)
        '    Return
        'End If

        Try
            Me.ProgRingShow(True)
            If Not Await _Odrive.ZalogujAsync(True) Then Return
        Catch ex As Exception
            bErr = ex.Message
        End Try

        If bErr <> "" Then
            Me.MsgBox(bErr)
            Me.ProgRingShow(False)
            Return
        End If

        Try
            Dim retMsg As String = Await _Odrive.SyncujAsync
            Me.MsgBox(retMsg)
        Catch ex As Exception
            bErr = ex.Message
        End Try

        If bErr <> "" Then
            Me.MsgBox(bErr)
        Else

            If _Odrive.GetStatusPliku("pudelka.json") > 0 Then
                vblib.globalsy.glZnanePudelka.Load()
                PokazListe(LekiSortMode.typNazwa)
            End If

            If _Odrive.GetStatusPliku("substancje.json") > 0 Then
                vblib.globalsy.glZnaneSubstancje.Load()
            End If

            If _Odrive.GetStatusPliku("zestawy.json") > 0 Then
                App.WczytajZestawyLubImportuj(True)
            End If

            If _Odrive.GetStatusPliku("gifdec.json") > 0 Then
                ' reload decyzje
            End If
        End If

        Me.ProgRingShow(False)
    End Sub

#End Region


    Private Async Sub uiCheckWycofania_Click(sender As Object, e As RoutedEventArgs)
        If vblib.globalsy.glWycofaniaGIF Is Nothing Then
            vblib.globalsy.glWycofaniaGIF = New vblib.WycofaniaGIF(Windows.Storage.ApplicationData.Current.RoamingFolder.Path)
            vblib.globalsy.glWycofaniaGIF.LoadCache()
        End If

        If pkar.NetIsIPavailable(False) Then
            Me.ProgRingShow(True)
            ' w ten sposób pomijamy Toast (bo ImportNewDecyzje zwraca tekst do toastu
            Await vblib.globalsy.glWycofaniaGIF.ImportNewDecyzje(100)
            Me.ProgRingShow(False)
        End If

        Dim trafienia As String = vblib.globalsy.SprawdzWycofania(False)

        If trafienia Is Nothing Then
            Me.MsgBox("Nieudane sprawdzenie wycofań - nie znam leków bądź wycofań")
            Return
        End If

        If trafienia = "" Then
            Me.MsgBox("Żaden lek nie został wycofany")
            Return
        End If

        Me.MsgBox("Zmiany statusu leków:" & vbCrLf & trafienia)

    End Sub

    Private Sub uiFilter_TextChanged(sender As Object, e As TextChangedEventArgs)
        PokazListe()
    End Sub

#Region "konwersjaXMLdoJSON"

#If False Then

    Private Async Function KonwersjaXMLdoJSON() As Task
        Dim oSerSet As New Newtonsoft.Json.JsonSerializerSettings With {.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, .DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore}

        Await App.ZestawyLoad()
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(App.glZestawy, Newtonsoft.Json.Formatting.Indented, oSerSet)
        Await SaveStringToFile("zestawy.json", sTxt)

        Await App.ZnaneSubstancjeLoad()
        sTxt = Newtonsoft.Json.JsonConvert.SerializeObject(App.glZnaneSubstancje, Newtonsoft.Json.Formatting.Indented, oSerSet)
        Await SaveStringToFile("substancje.json", sTxt)

        Await App.ZnanePudelkaLoad
        sTxt = Newtonsoft.Json.JsonConvert.SerializeObject(App.glZnanePudelka, Newtonsoft.Json.Formatting.Indented, oSerSet)
        Await SaveStringToFile("pudelka.json", sTxt)

    End Function

    Private Async Function SaveStringToFile(fname As String, contn As String) As Task

        Dim path As String = (Await App.GetRoamingFile(fname, True)).Path
        IO.File.WriteAllText(path, contn)
    End Function
#End If

#End Region

End Class

#Region "konwertery XAML"

Public Class KonwersjaTarczkaOnOff
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim status As GIFstatus = CType(value, GIFstatus)

        If status = GIFstatus.Wstrzymany OrElse status = GIFstatus.Wycofany Then
            Return Visibility.Visible
        End If

        Return Visibility.Collapsed

    End Function
End Class

Public Class KonwersjaTarczkaZnak
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim status As GIFstatus = CType(value, GIFstatus)

        Select Case status
            Case GIFstatus.Wstrzymany
                Return "⚠"
            Case GIFstatus.Wycofany
                Return "🛑"
        End Select

        Return ""
    End Function
End Class

Public Class KonwersjaTarczkaKolor
    Inherits ValueConverterOneWaySimple

    Private Shared _czerwony As New SolidColorBrush(Windows.UI.Colors.Red)
    Private Shared _zolty As New SolidColorBrush(Windows.UI.Colors.Yellow)
    Private Shared _czarny As New SolidColorBrush(Windows.UI.Colors.Black)

    Protected Overrides Function Convert(value As Object) As Object
        Dim status As GIFstatus = CType(value, GIFstatus)

        Select Case status
            Case GIFstatus.Wstrzymany
                Return _zolty
            Case GIFstatus.Wycofany
                Return _czerwony
        End Select

        Return _czarny

    End Function
End Class

#End Region