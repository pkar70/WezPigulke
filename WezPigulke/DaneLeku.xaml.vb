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
        Me.InitDialogs

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

    Private Function GetPdfFilename(pudelko As vblib.JednoPudelko, bCharakt As Boolean)
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
    Private Async Function TryShowPdf(oFold As Windows.Storage.StorageFolder, fname As String) As Task(Of Boolean)

        Dim oFile As Windows.Storage.StorageFile = Nothing
        ' Dim oCos = oFold.TryGetItemAsync(sIntNum & ".pdf")
        oFile = Await oFold.TryGetItemAsync(fname)

        If oFile Is Nothing Then Return False

        Windows.System.Launcher.LaunchFileAsync(oFile)
        Return True
    End Function

    Private Async Sub PokazPDF(bCharakt As Boolean)

        Dim fname As String = GetPdfFilename(ContextAsPudelko, bCharakt)

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        If oFold Is Nothing Then
            Me.MsgBox("ERROR: cannot open LocalFolder?")
            Return
        End If

        If Await TryShowPdf(oFold, fname) Then Return

        If Not vblib.GetSettingsBool("cacheDataHere") Then Return
        If Not Await Me.DialogBoxYNAsync("Ściągnąć plik?") Then Return

        Dim sUrl As String = "https://rejestry.ezdrowie.gov.pl/medicinal-products/33004/leaflet/" & ContextAsPudelko.IDrpl
        sUrl &= If(bCharakt, "/characteristic", "/leaflet")

        Dim bErr As Boolean = False
        Dim oFile As Windows.Storage.StorageFile = Nothing
        ' Dim oCos = oFold.TryGetItemAsync(sIntNum & ".pdf")

        ' ściągamy
        oFile = Await oFold.CreateFileAsync(fname)
        ' https://stackoverflow.com/questions/49431603/download-pdf-to-localfolder-in-uwp
        Dim oDownloader As New Windows.Networking.BackgroundTransfer.BackgroundDownloader
        Dim oDownload = oDownloader.CreateDownload(New Uri(sUrl), oFile)
        Await oDownload.StartAsync()
        Me.MsgBox("Zainicjowałem ściąganie, poczekaj chwilę i spróbuj ponownie")

        ' http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-c
        ' http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-u
        ' https://rejestry.ezdrowie.gov.pl/medicinal-products/33004/leaflet
        ' https://rejestry.ezdrowie.gov.pl/medicinal-products/33004/characteristic
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
