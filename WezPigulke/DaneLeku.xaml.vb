
Imports pkar.UI.Extensions

Public NotInheritable Class DaneLeku
    Inherits Page

    ' pokazuje dane z parametru wywołania - barcode
    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        Dim msBarCode As String = ""
        If e Is Nothing Then Return
        Dim sTmp As String = TryCast(e.Parameter, String)
        If sTmp Is Nothing Then Return
        msBarCode = sTmp

        For Each oItem As vblib.JednoPudelko In vblib.globalsy.glZnanePudelka
            If oItem.sBarcode = msBarCode OrElse
                oItem.sOpakowania.Contains(msBarCode) Then 'OrElse

                oItem.sBarcode = msBarCode
                Me.DataContext = oItem
                Exit For
            End If
        Next

    End Sub

    Private Function ContextAsPudelko() As vblib.JednoPudelko
        Return TryCast(DataContext, vblib.JednoPudelko)
    End Function

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        'Me.InitDialogs

        'oItem.sDawneOpakowania.Contains(msBarCode) Then

        uiKodATC.NavigateUri = New Uri("https://www.whocc.no/atc_ddd_index/?code=" & ContextAsPudelko.sKodATC)
        'uiDawneOpakowania.Text = oItem.sDawneOpakowania.Replace("#", vbCrLf).Trim

        uiInterAlk.Text = "?"
        uiInterJedz.Text = "?"

        For Each oSubst As vblib.JednaSubstancja In vblib.globalsy.glZnaneSubstancje
            If oSubst.sNazwa.ToLower = ContextAsPudelko.sNazwaCzynna.ToLower Then
                uiInterAlk.Text = oSubst.sInterAlk
                uiInterJedz.Text = oSubst.sInterJedz
                Exit For
            End If
        Next

    End Sub

    Private Shared Function GetPdfFilename(pudelko As vblib.JednoPudelko, bCharakt As Boolean)
        If bCharakt Then
            Return pudelko.IDrpl & "-c.pdf"
        Else
            Return pudelko.IDrpl & "-u.pdf"
        End If
    End Function

    ''' <summary>
    ''' spróbuj pokazać PDFa, TRUE: nie podejmuj dalszych kroków
    ''' </summary>
    ''' <param name="fname"></param>
    ''' <returns></returns>
    Private Shared Async Function TryShowPdf(oFold As Windows.Storage.StorageFolder, fname As String) As Task(Of Boolean)

        Dim oFile As Windows.Storage.StorageFile = Nothing
        ' Dim oCos = oFold.TryGetItemAsync(sIntNum & ".pdf")
        oFile = Await oFold.TryGetItemAsync(fname)

        If oFile Is Nothing Then Return False

        Windows.System.Launcher.LaunchFileAsync(oFile)
        Return True
    End Function



    ''' <summary>
    ''' Pokaż PDFa lub zainicjuj jego ściąganie; ret Empty lub errmessage
    ''' </summary>
    ''' <param name="oItem"></param>
    ''' <param name="bCharakt">albo 'characteristic' albo 'leaflet'</param>
    ''' <returns>Empty na OK, lub errmessage</returns>
    Public Shared Async Function PokazPDF(oItem As vblib.JednoPudelko, bCharakt As Boolean) As Task(Of String)
        If oItem Is Nothing Then Return "Bad call: PokazPDF(null...)"

        Dim fname As String = GetPdfFilename(oItem, bCharakt)

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder

        If Await TryShowPdf(oFold, fname) Then Return ""

        Dim oUri As New Uri($"https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/{oItem.IDrpl}/{If(bCharakt, "characteristic", "leaflet")}")

        If Not vblib.GetSettingsBool("cacheDataHere") OrElse Not Await vblib.DialogBoxYNAsync("Ściągnąć plik?") Then
            oUri.OpenBrowser
            Return ""
        End If

        Dim bErr As Boolean = False
        Dim oFile As Windows.Storage.StorageFile = Nothing
        ' Dim oCos = oFold.TryGetItemAsync(sIntNum & ".pdf")

        ' ściągamy
        oFile = Await oFold.CreateFileAsync(fname)
        ' https://stackoverflow.com/questions/49431603/download-pdf-to-localfolder-in-uwp
        Dim oDownloader As New Windows.Networking.BackgroundTransfer.BackgroundDownloader
        Dim oDownload = oDownloader.CreateDownload(oUri, oFile)
        Await oDownload.StartAsync()
        vblib.MsgBox("Zainicjowałem ściąganie, poczekaj chwilę i spróbuj ponownie")

        ' http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-c
        ' http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-u
        ' https://rejestry.ezdrowie.gov.pl/medicinal-products/33004/leaflet
        ' https://rejestry.ezdrowie.gov.pl/medicinal-products/33004/characteristic
    End Function



    Private Async Sub PokazPDF(bCharakt As Boolean)
        Dim errMsg As String = Await PokazPDF(ContextAsPudelko, False)
        If String.IsNullOrWhiteSpace(errMsg) Then Return
        Me.MsgBox(errMsg)
    End Sub

    Private Sub uiShowUlotka_Click(sender As Object, e As RoutedEventArgs)
        PokazPDF(False)
    End Sub

    Private Sub uiShowCharakter_Click(sender As Object, e As RoutedEventArgs)
        PokazPDF(True)
    End Sub

    Private Async Sub uiInterJedz_DoubleTapped(sender As Object, e As DoubleTappedRoutedEventArgs)
        InterakcjePokazSciagnij(True)
    End Sub

    Private Async Sub uiInterAlk_DoubleTapped(sender As Object, e As DoubleTappedRoutedEventArgs)
        InterakcjePokazSciagnij(False)
    End Sub

    Private Async Function InterakcjePokazSciagnij(bJedzonko As Boolean) As Task
        If bJedzonko And uiInterJedz.Text <> "?" Then Return
        If Not bJedzonko And uiInterAlk.Text <> "?" Then Return

        ' jeśli tu doszliśmy, to znaczy że nie było substancji - więc trzeba ją dodać (zamienić na ID)
        Dim subst As vblib.JednaSubstancja = Await ContextAsPudelko.ZnajdzSubstancje()

        ' ... i ściągnąć z WWW
        Dim interakcje As String = Await subst.GetWebInterakcje(bJedzonko)

        If bJedzonko Then
            uiInterJedz.Text = interakcje
        Else
            uiInterAlk.Text = interakcje
        End If

        vblib.globalsy.glZnaneSubstancje.Save()

    End Function


End Class
