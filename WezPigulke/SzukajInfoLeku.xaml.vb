' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

'a.różne źródła
'        i.z zestawu
'        ii.z nazwy, z substancji
'        iii.z barcode
'        iv.z OCR barcode
'    b.one są roam
'    c.info z pub.rejestrymedyczne.csioz.gov.pl
'    d.interakcje
'e.dodaj lek stały, czasowy, sprawdź lek

' broncho-vaxom: 5909990062928 , ATC R07AX 
' ulotka: http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-u
' charakt: http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-c
' strona leku: http://pub.rejestrymedyczne.csioz.gov.pl/ProduktSzczegoly.aspx?id=22265

' szukanie: ATC code, czyli zastosowanie
' https://en.wikipedia.org/wiki/Anatomical_Therapeutic_Chemical_Classification_System
' z ATC jest dawkowanie :) https://www.whocc.no/atc_ddd_index/?code=C07AB07

' nazwa, common nazwa, substancja czynna, ale głównie EAN

' interakcje mogą być z substancji czynnej... o tyle dobrze :)

' mozna z pub.rejestry pojsc o jedna stronę dalej, i mieć:
' (active substancja) Szczepionka wieloważna
' Kod ATC - wtedy go rozpisac
' opakowania istniejące, z Rp - recepta
' opakowania wycofane

' mozna jakos robic cache danych (wedle EAN na przyklad)
' gdy GetSettingsBool("cacheDataHere")

' ListView: opcje: dodaj do zapamietanych (przechowywanych na dysku)

' ktomalek - mozna z nazwy szukac zamiennikow, sprawdzac w aptekach w ogóle! - ale bez cen


Public NotInheritable Class SzukajInfoLeku
    Inherits Page

    Private moHttp As Windows.Web.Http.HttpClient

    Private Sub Progresuj(bShow As Boolean)
        If bShow Then
            Dim dVal As Double
            dVal = (Math.Min(uiGrid.ActualHeight, uiGrid.ActualWidth)) / 2
            uiProcesuje.Width = dVal
            uiProcesuje.Height = dVal

            uiProcesuje.Visibility = Visibility.Visible
            uiProcesuje.IsActive = True
        Else
            uiProcesuje.Visibility = Visibility.Collapsed
            uiProcesuje.IsActive = False
        End If
    End Sub

    Private Function WyciagnijValueLista(sPage As String, sVarName As String) As String
        Dim iInd As Integer
        iInd = sPage.IndexOf(sVarName)
        If iInd < 1 Then Return ""
        Dim sRet As String = sPage.Substring(iInd, 1000)
        iInd = sRet.IndexOf("value=")
        sRet = sRet.Substring(iInd + 7)
        iInd = sRet.IndexOf("""")
        If iInd < 1 Then Return ""
        Return sRet.Substring(0, iInd)
    End Function

    Private Async Function ZapytajRejestrySpis() As Task(Of String)
        If moHttp Is Nothing Then
            moHttp = New Windows.Web.Http.HttpClient
        End If

        Dim sUri As String = "http://pub.rejestrymedyczne.csioz.gov.pl"
        Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing
        Dim sResp As String = ""
        oResp = Await moHttp.GetAsync(New Uri(sUri))
        sResp = Await oResp.Content.ReadAsStringAsync

        Await Task.Delay(500)   ' poczekajmy

        Dim oParamsy As Dictionary(Of String, String) = New Dictionary(Of String, String)
        oParamsy.Add("substancjaCzynnaTXTSearch", uiSubst.Text)
        oParamsy.Add("kodATCTXTSearch", uiATC.Text)
        Dim sEAN As String = uiEAN.Text
        If sEAN = "590999" Then sEAN = ""
        oParamsy.Add("kodEANTXTSearch", sEAN)
        oParamsy.Add("NazwaTXTSearch", uiNazwa.Text)
        oParamsy.Add("RodzPrep", "L")

        ' dodajemy pozostale
        oParamsy.Add("NazwaPodOdpTXTSearch", "")
        oParamsy.Add("NumerPozTXTSearch", "")
        oParamsy.Add("NazwaPowStoTXTSearch", "")
        oParamsy.Add("btnSearch", "Szukaj")
        oParamsy.Add("__EVENTARGUMENT", WyciagnijValueLista(sResp, "__EVENTARGUMENT"))
        oParamsy.Add("__EVENTTARGET", WyciagnijValueLista(sResp, "__EVENTTARGET"))
        oParamsy.Add("__EVENTVALIDATION", WyciagnijValueLista(sResp, "__EVENTVALIDATION"))
        oParamsy.Add("__VIEWSTATE", WyciagnijValueLista(sResp, "__VIEWSTATE"))
        oParamsy.Add("__VIEWSTATEGENERATOR", WyciagnijValueLista(sResp, "__VIEWSTATEGENERATOR"))

        Dim oHttpCont As Windows.Web.Http.HttpFormUrlEncodedContent

        oHttpCont = New Windows.Web.Http.HttpFormUrlEncodedContent(oParamsy)

        'Dim oHttpCont = New Windows.Web.Http.HttpStringContent(sData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded")

        oResp = Await moHttp.PostAsync(New Uri(sUri), oHttpCont)

        sResp = Await oResp.Content.ReadAsStringAsync
        If sResp.IndexOf("ToDetailsLink") < 1 Then
            Await DialogBox("raczej nie ma zawartosci")
            ClipPut(sResp)
            Return ""
        End If

        Return sResp
    End Function

    Private mlPudelka As Collection(Of JednoPudelko)

    Private Sub ExtractListePudelek(sPage As String, sBarCode As String)
        mlPudelka = New Collection(Of JednoPudelko)

        Dim iInd As Integer

        iInd = sPage.IndexOf("ToDetailsLink")
        While iInd > 0
            '    <a id = "GridView1_ToDetailsLink_0" Class="underline" href="ProduktSzczegoly.aspx?id=15973" target="_blank">Ampril 10 mg tabletki<span Class='sr-only'>12097</span></a>
            '                                        </td><td>Ramiprilum</td><td>10 mg</td><td>tabletki</td><td>12097</td><td>Bezterminowe<span class='translation'>For unlimited period</span></td><td>Krka, d.d., Novo mesto</td><td>MRP</td>
            sPage = sPage.Substring(iInd + 10)
            Dim oNew As JednoPudelko = New JednoPudelko

            iInd = sPage.IndexOf("ProduktSzczegoly.aspx?id")
            If iInd < 0 Then Return
            sPage = sPage.Substring(iInd)
            '    ProduktSzczegoly.aspx?id=15973" target="_blank">Ampril 10 mg tabletki<span Class='sr-only'>12097</span></a>
            '                                        </td><td>Ramiprilum</td><td>10 mg</td><td>tabletki</td><td>12097</td><td>Bezterminowe<span class='translation'>For unlimited period</span></td><td>Krka, d.d., Novo mesto</td><td>MRP</td>

            iInd = sPage.IndexOf("""")
            oNew.sDetailsLink = sPage.Substring(0, iInd)


            iInd = sPage.IndexOf(">")
            If iInd < 0 Then Return
            sPage = sPage.Substring(iInd + 1)
            '    Ampril 10 mg tabletki<span Class='sr-only'>12097</span></a>
            '                                        </td><td>Ramiprilum</td><td>10 mg</td><td>tabletki</td><td>12097</td><td>Bezterminowe<span class='translation'>For unlimited period</span></td><td>Krka, d.d., Novo mesto</td><td>MRP</td>
            iInd = sPage.IndexOf("<")
            oNew.sNazwa = sPage.Substring(0, iInd)

            iInd = sPage.IndexOf("<td")
            If iInd < 0 Then Return
            iInd = sPage.IndexOf(">", iInd)
            sPage = sPage.Substring(iInd + 1)
            ' Ramiprilum</td><td>10 mg</td><td>tabletki</td><td>12097</td><td>Bezterminowe<span class='translation'>For unlimited period</span></td><td>Krka, d.d., Novo mesto</td><td>MRP</td>
            iInd = sPage.IndexOf("</td")
            oNew.sNazwaPowszechna = RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<td")
            If iInd < 0 Then Return
            iInd = sPage.IndexOf(">", iInd)
            sPage = sPage.Substring(iInd + 1)
            ' 10 mg</td><td>tabletki</td><td>12097</td><td>Bezterminowe<span class='translation'>For unlimited period</span></td><td>Krka, d.d., Novo mesto</td><td>MRP</td>
            iInd = sPage.IndexOf("</td")
            oNew.sMoc = RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<td")
            If iInd < 0 Then Return
            iInd = sPage.IndexOf(">", iInd)
            sPage = sPage.Substring(iInd + 1)
            ' tabletki</td><td>12097</td><td>Bezterminowe<span class='translation'>For unlimited period</span></td><td>Krka, d.d., Novo mesto</td><td>MRP</td>
            iInd = sPage.IndexOf("</td")
            oNew.sPostac = RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<td")
            If iInd < 0 Then Return
            iInd = sPage.IndexOf(">", iInd)
            sPage = sPage.Substring(iInd + 1)
            ' 12097</td><td>Bezterminowe<span class='translation'>For unlimited period</span></td><td>Krka, d.d., Novo mesto</td><td>MRP</td>
            iInd = sPage.IndexOf("</td")
            oNew.sPozwolenie = RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<td")
            If iInd < 0 Then Return
            iInd = sPage.IndexOf(">", iInd)
            sPage = sPage.Substring(iInd + 1)
            ' Bezterminowe<span class='translation'>For unlimited period</span></td><td>Krka, d.d., Novo mesto</td><td>MRP</td>
            Dim iInd1 As Integer = sPage.IndexOf("<span")
            Dim iInd2 As Integer = sPage.IndexOf("</td")
            If iInd1 = -1 Then iInd1 = 9999
            iInd = Math.Min(iInd1, iInd2)

            oNew.sWaznosc = RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<td")
            If iInd < 0 Then Return
            iInd = sPage.IndexOf(">", iInd)
            sPage = sPage.Substring(iInd + 1)
            ' Krka, d.d., Novo mesto</td><td>MRP</td>
            iInd = sPage.IndexOf("</td")
            oNew.sPodmiot = RemoveHtmlTags(sPage.Substring(0, iInd))

            iInd = sPage.IndexOf("<td")
            If iInd < 0 Then Return
            iInd = sPage.IndexOf(">", iInd)
            sPage = sPage.Substring(iInd + 1)
            ' MRP</td>
            iInd = sPage.IndexOf("</td")
            oNew.sProcedura = RemoveHtmlTags(sPage.Substring(0, iInd))

            oNew.sCreated = Date.Now.ToString("yyyyMMdd HH:mm")
            oNew.sBarcode = sBarCode
            mlPudelka.Add(oNew)

            iInd = sPage.IndexOf("ToDetailsLink")
        End While

    End Sub

    Private Async Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)

        Dim bCos As Boolean = False
        If uiSubst.Text <> "" Then bCos = True
        If uiATC.Text <> "" Then bCos = True
        If uiEAN.Text <> "" AndAlso uiEAN.Text <> "590999" Then
            If uiEAN.Text.Length <> 13 Then
                Await DialogBox("ERROR: EAN <> 13 chars!")
                Exit Sub
            End If
            bCos = True
        End If
        If uiNazwa.Text <> "" Then bCos = True

        If Not bCos Then
            Await DialogBox("Jakiś parametr szukania być musi")
            Return
        End If

        uiSearch.IsEnabled = False

        Progresuj(True)

        Dim sSpis As String = Await ZapytajRejestrySpis()
        ExtractListePudelek(sSpis, uiEAN.Text)

        FullFormShow(False)
        If uiGrid.ActualWidth > 600 Then
            uiListSzeroki.Visibility = Visibility.Visible
            uiListWaski.Visibility = Visibility.Collapsed
            uiListSzeroki.ItemsSource = From c In mlPudelka
        Else
            uiListSzeroki.Visibility = Visibility.Collapsed
            uiListWaski.Visibility = Visibility.Visible
            uiListWaski.ItemsSource = From c In mlPudelka
        End If ' wedle uiGrid

        Progresuj(False)

        uiSearch.IsEnabled = True
    End Sub

    Private Sub FullFormShow(bShow As Boolean)
        If bShow Then
            uiLabelSubst.Visibility = Visibility.Visible
            uiLabelATC.Visibility = Visibility.Visible
            uiSubst.Visibility = Visibility.Visible
            uiATC.Visibility = Visibility.Visible
        Else
            uiLabelSubst.Visibility = Visibility.Collapsed
            uiLabelATC.Visibility = Visibility.Collapsed
            uiSubst.Visibility = Visibility.Collapsed
            uiATC.Visibility = Visibility.Collapsed
        End If

    End Sub


    Private Sub uiFullForm_Click(sender As Object, e As RoutedEventArgs)
        FullFormShow(uiFullForm.IsChecked)
    End Sub

    Private Async Sub uiScanBarCode_Click(sender As Object, e As RoutedEventArgs)
        ' tu bedzie najciekawsze :)
        ' Camera-based
        'https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/BarcodeScanner
        ' nieprawda, jest tylko gdy skaner ma mozliwosc preview
        'Dim oWatch = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(Windows.Devices.PointOfService.BarcodeScanner.GetDeviceSelector())
        'Dim oList = Await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.PointOfService.BarcodeScanner.GetDeviceSelector())
        '        Dim oScanColl As Collection(Of Windows.Devices.PointOfService.BarcodeScanner.BarcodeScannerInfo)

        'Github biblioteka
        'https://github.com/nblockchain/ZXing.Net.Xamarin

        Dim oScanner As ZXing.Mobile.MobileBarcodeScanner
        oScanner = New ZXing.Mobile.MobileBarcodeScanner(Me.Dispatcher)
        'Tell our scanner to use the default overlay 
        oScanner.UseCustomOverlay = False
        ' //We can customize the top And bottom text of our default overlay 
        oScanner.TopText = "Hold camera up to barcode"
        oScanner.BottomText = "Camera will automatically scan barcode" & vbCrLf & "Press the 'Back' button to Cancel"
        Dim oRes = Await oScanner.Scan()

        If oRes Is Nothing Then Return

        If oRes.BarcodeFormat <> ZXing.BarcodeFormat.EAN_13 Then
            Await DialogBox("To nie wygląda na kod EAN13")
            Return
        End If

        uiEAN.Text = oRes.Text
        App.gsEAN = uiEAN.Text
        Dim sHistoryEAN As String = GetSettingsString("lastEANs")
        sHistoryEAN = uiEAN.Text & "|" & sHistoryEAN
        If sHistoryEAN.Length > 14 * GetSettingsInt("maxScannedEANs", 10) Then
            sHistoryEAN = sHistoryEAN.Substring(0, 14 * GetSettingsInt("maxScannedEANs", 10))
        End If
        SetSettingsString("lastEANs", sHistoryEAN, True)
        'uiNazwa.Text = "to jest tex"
        ' włączanie żarówki?

    End Sub

    Private Sub uiGoWeb_Click(sender As Object, e As RoutedEventArgs)
        Dim oGrid As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oGrid IsNot Nothing Then
            Dim oItem As JednoPudelko = TryCast(oGrid.DataContext, JednoPudelko)
            Dim sUri As String = "http://pub.rejestrymedyczne.csioz.gov.pl/"
            sUri = sUri & oItem.sDetailsLink   ' "ProduktSzczegoly.aspx?id=22265"
            OpenBrowser(sUri, False)
        End If
    End Sub

    Private Sub uiShowProcedures_Click(sender As Object, e As RoutedEventArgs)
        ' za: https://getmedi.pl/news/25/procedury-rejestracyjne-wsrod-lekow-refundowanych
        Dim sUri As String = "https://getmedi.pl/news/25/procedury-rejestracyjne-wsrod-lekow-refundowanych"
        OpenBrowser(sUri, False)
    End Sub

    Private Sub Page_GotFocus(sender As Object, e As RoutedEventArgs)
        If App.gsEAN <> "" Then uiEAN.Text = App.gsEAN
        App.gsEAN = ""
    End Sub

    Public Sub WypelnijMenuEAN(oMenu As MenuFlyout)
        Dim sEANy As String = GetSettingsString("lastEANs")
        Dim aEANy As String() = sEANy.Split("|")

        Dim sEANyAdded As String = ""
        For Each sEAN As String In aEANy
            If sEAN.Length > 10 Then
                If Not sEANyAdded.Contains(sEAN) Then
                    Dim oNew As MenuFlyoutItem = New MenuFlyoutItem
                    oNew.Text = sEAN
                    AddHandler oNew.Click, AddressOf ChooseEAN
                    oMenu.Items.Add(oNew)
                    sEANyAdded = sEANyAdded & "|"
                End If
            End If
        Next
    End Sub

    Private Sub ChooseEAN(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return

        uiEAN.Text = oMFI.Text
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiEAN.Text = App.gsEAN
        WypelnijMenuEAN(uiLastEAN)
    End Sub

    Private Sub UiLabelEAN_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles uiLabelEAN.Tapped
        ' bo 1809 nie pokazuje FLyout'a , wiec trzeba inną mozliwosc pokazania
        uiLabelEAN.ContextFlyout.ShowAt(uiLabelEAN)
    End Sub

    Private Function WyciagnijValueDetails(sPage As String, sVarName As String) As String
        Dim iInd As Integer
        iInd = sPage.IndexOf(sVarName)
        If iInd < 1 Then Return ""
        Dim sRet As String = sPage.Substring(iInd, 1000)
        iInd = sRet.IndexOf("<dd>")
        sRet = sRet.Substring(iInd)
        iInd = sRet.IndexOf("</dd")
        sRet = sRet.Substring(0, iInd)
        If sVarName.ToLower.StartsWith("opak") Then sRet = sRet.Replace("</li>", "#")
        Return RemoveHtmlTags(sRet)
    End Function

    Private Async Sub uiDodajTenLek_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return

        Dim oItem As JednoPudelko = TryCast(oMFI.DataContext, JednoPudelko)
        If oItem Is Nothing Then Return

        For Each oTmp As JednoPudelko In App.glZnanePudelka
            If oTmp.sBarcode = oItem.sBarcode Then
                Await DialogBoxYN("Już to znamy!")
                Return
            End If
        Next

        Progresuj(True)

        ' uzupelnij dane
        Dim sUri As String = "http://pub.rejestrymedyczne.csioz.gov.pl/"
        sUri = sUri & oItem.sDetailsLink   ' "ProduktSzczegoly.aspx?id=22265"

        Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing
        Dim sResp As String = ""
        oResp = Await moHttp.GetAsync(New Uri(sUri))
        sResp = Await oResp.Content.ReadAsStringAsync

        Await Task.Delay(500)   ' poczekajmy

        Dim oParamsy As Dictionary(Of String, String) = New Dictionary(Of String, String)
        oParamsy.Add("Submit", "Submit")
        oParamsy.Add("__EVENTARGUMENT", WyciagnijValueLista(sResp, "__EVENTARGUMENT"))
        oParamsy.Add("__EVENTTARGET", WyciagnijValueLista(sResp, "__EVENTTARGET"))
        oParamsy.Add("__EVENTVALIDATION", WyciagnijValueLista(sResp, "__EVENTVALIDATION"))
        oParamsy.Add("__VIEWSTATE", WyciagnijValueLista(sResp, "__VIEWSTATE"))
        oParamsy.Add("__VIEWSTATEGENERATOR", WyciagnijValueLista(sResp, "__VIEWSTATEGENERATOR"))

        Dim oHttpCont As Windows.Web.Http.HttpFormUrlEncodedContent
        oHttpCont = New Windows.Web.Http.HttpFormUrlEncodedContent(oParamsy)

        sResp = ""
        oResp = Await moHttp.PostAsync(New Uri(sUri), oHttpCont)

        sResp = Await oResp.Content.ReadAsStringAsync

        oItem.sNazwaCzynna = WyciagnijValueDetails(sResp, "SubstancjaTXT")
        oItem.sKodATC = WyciagnijValueDetails(sResp, "KodATCTXT")

        Dim iInd As Integer
        oItem.sOpakowania = WyciagnijValueDetails(sResp, "OpakowaniaTXT")
        If oItem.sBarcode = "" Then
            iInd = oItem.sOpakowania.IndexOf(" ,")
            If iInd > 0 Then
                oItem.sBarcode = oItem.sOpakowania.Substring(iInd + 2)
                iInd = oItem.sBarcode.IndexOf(",")
                iInd = Math.Max(14, iInd)
                If iInd > 0 Then oItem.sBarcode = oItem.sBarcode.Substring(0, iInd)
            End If
        End If

        oItem.sDawneOpakowania = WyciagnijValueDetails(sResp, "OpakowaniaSkasowaneTXT")
        If oItem.sBarcode = "" Then
            iInd = oItem.sDawneOpakowania.IndexOf(" ,")
            If iInd > 0 Then
                oItem.sBarcode = oItem.sDawneOpakowania.Substring(iInd + 2)
                iInd = oItem.sBarcode.IndexOf(",")
                iInd = Math.Max(14, iInd)
                If iInd > 0 Then oItem.sBarcode = oItem.sBarcode.Substring(0, iInd)
            End If
        End If

        ' zapisz dane
        App.glZnanePudelka.Add(oItem)
        Await App.ZnanePudelkaSave()

        Progresuj(False)
    End Sub
End Class
