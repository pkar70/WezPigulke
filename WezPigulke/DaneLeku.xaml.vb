' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class DaneLeku
    Inherits Page

    ' pokazuje dane z parametru wywołania - barcode

    Private msBarCode As String
    Private msLink As String

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        msBarCode = ""
        If e Is Nothing Then Return
        Dim sTmp As String = TryCast(e.Parameter, String)
        If sTmp Is Nothing Then Return
        msBarCode = sTmp
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiNazwa.Text = msBarCode

        For Each oItem As JednoPudelko In App.glZnanePudelka
            If oItem.sBarcode = msBarCode OrElse
                oItem.sOpakowania.Contains(msBarCode) OrElse
                oItem.sDawneOpakowania.Contains(msBarCode) Then

                uiNazwa.Text = oItem.sNazwa
                uiNazwaPowszechna.Text = oItem.sNazwaPowszechna
                uiSubstancjaCzynna.Text = oItem.sNazwaCzynna
                uiPostac.Text = oItem.sPostac
                uiMoc.Text = oItem.sMoc
                uiPozwolenie.Text = oItem.sPozwolenie
                uiWaznosc.Text = oItem.sWaznosc
                uiPodmiot.Text = oItem.sPodmiot
                uiProcedura.Text = oItem.sProcedura
                uiKodATC.Content = oItem.sKodATC
                uiKodATC.NavigateUri = New Uri("https://www.whocc.no/atc_ddd_index/?code=" & oItem.sKodATC)
                uiBarcode.Text = oItem.sBarcode
                uiOpakowania.Text = oItem.sOpakowania.Replace("#", vbCrLf).Trim
                uiDawneOpakowania.Text = oItem.sDawneOpakowania.Replace("#", vbCrLf).Trim

                uiInterAlk.Text = "?"
                uiInterJedz.Text = "?"

                For Each oSubst As JednaSubstancja In App.glZnaneSubstancje
                    If oSubst.sNazwa.ToLower = oItem.sNazwaCzynna.ToLower Then
                        uiInterAlk.Text = oSubst.sInterAlk
                        uiInterJedz.Text = oSubst.sInterJedz
                        Exit For
                    End If
                Next

                msLink = oItem.sDetailsLink
            End If

        Next

    End Sub

    Private Async Sub PokazPDF(bCharakt As Boolean)
        Dim sIntNum As String = msLink
        If msLink = "" Then Return
        ' ProduktSzczegoly.aspx?id=22265
        Dim iInd As Integer
        iInd = sIntNum.IndexOf("=")
        sIntNum = sIntNum.Substring(iInd + 1)

        If bCharakt Then
            sIntNum &= "-c"
        Else
            sIntNum &= "-u"
        End If

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        If oFold Is Nothing Then
            Await DialogBox("ERROR: cannot open LocalFolder?")
            Return
        End If

        Dim bErr As Boolean = False
        Dim oFile As Windows.Storage.StorageFile = Nothing
        ' Dim oCos = oFold.TryGetItemAsync(sIntNum & ".pdf")
        oFile = Await oFold.TryGetItemAsync(sIntNum & ".pdf")
        If oFile Is Nothing Then
            Dim sUrl As String = "http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=" & sIntNum


            If GetSettingsBool("cacheDataHere") AndAlso Await DialogBoxYN("Ściągnąć plik?") Then
                ' ściągamy
                oFile = Await oFold.CreateFileAsync(sIntNum & ".pdf")
                ' https://stackoverflow.com/questions/49431603/download-pdf-to-localfolder-in-uwp
                Dim oDownloader As Windows.Networking.BackgroundTransfer.BackgroundDownloader = New Windows.Networking.BackgroundTransfer.BackgroundDownloader
                Dim oDownload = oDownloader.CreateDownload(New Uri(sUrl), oFile)
                Await oDownload.StartAsync()
                DialogBox("Zainicjowałem ściąganie, poczekaj chwilę i spróbuj ponownie")
            Else
                ' pokazujemy w browser z linku
                OpenBrowser(sUrl, False)
                End If
            Else
                ' pokazujemy z dysku
                Windows.System.Launcher.LaunchFileAsync(oFile)
        End If

        ' http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-c
        ' http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-u
    End Sub

    Private Sub uiShowUlotka_Click(sender As Object, e As RoutedEventArgs)
        PokazPDF(False)
    End Sub

    Private Sub uiShowCharakter_Click(sender As Object, e As RoutedEventArgs)
        PokazPDF(True)
    End Sub
End Class
