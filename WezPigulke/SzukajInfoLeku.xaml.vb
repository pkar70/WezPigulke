
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


Imports pkar.UI.Extensions

Public NotInheritable Class SzukajInfoLeku
    Inherits Page

    Private Sub Page_GotFocus(sender As Object, e As RoutedEventArgs)
        If App.gsEAN <> "" Then uiEAN.Text = App.gsEAN
        App.gsEAN = ""
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        uiEAN.Text = App.gsEAN
        WypelnijMenuEAN(uiLastEAN)
    End Sub


    Private Async Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)

        Dim bCos As Boolean = False
        If uiSubst.Text <> "" Then bCos = True
        If uiATC.Text <> "" Then bCos = True
        If uiEAN.Text <> "" AndAlso uiEAN.Text <> "590999" Then
            If uiEAN.Text.Length <> 13 Then
                Me.MsgBox("ERROR: EAN <> 13 chars!")
                Exit Sub
            End If
            bCos = True
        End If
        If uiNazwa.Text <> "" Then bCos = True

        If Not bCos Then
            Me.MsgBox("Jakiś parametr szukania być musi")
            Return
        End If

        uiSearch.IsEnabled = False

        Me.ProgRingShow(True)

        Dim lPudelka As List(Of vblib.JednoPudelko) = Await vblib.SzukajInfoLeku.GetListaPudelek(uiEAN.Text, uiNazwa.Text, uiSubst.Text, uiATC.Text)

        FullFormShow(False)
        If uiGrid.ActualWidth > 600 Then
            uiListSzeroki.Visibility = Visibility.Visible
            uiListWaski.Visibility = Visibility.Collapsed
            uiListSzeroki.ItemsSource = From c In lPudelka
        Else
            uiListSzeroki.Visibility = Visibility.Collapsed
            uiListWaski.Visibility = Visibility.Visible
            uiListWaski.ItemsSource = From c In lPudelka
        End If ' wedle uiGrid

        Me.ProgRingShow(False)

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
            Me.MsgBox("To nie wygląda na kod EAN13")
            Return
        End If

        uiEAN.Text = oRes.Text
        App.gsEAN = uiEAN.Text
        Dim sHistoryEAN As String = vblib.GetSettingsString("lastEANs")
        sHistoryEAN = uiEAN.Text & "|" & sHistoryEAN
        If sHistoryEAN.Length > 14 * vblib.GetSettingsInt("maxScannedEANs", 10) Then
            sHistoryEAN = sHistoryEAN.Substring(0, 14 * vblib.GetSettingsInt("maxScannedEANs", 10))
        End If
        vblib.SetSettingsString("lastEANs", sHistoryEAN, True)
        'uiNazwa.Text = "to jest tex"
        ' włączanie żarówki?

    End Sub

    ' niestety, teraz nie ma strony per lek
    'Private Sub uiGoWeb_Click(sender As Object, e As RoutedEventArgs)
    '    Dim oGrid As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
    '    If oGrid IsNot Nothing Then
    '        Dim oItem As JednoPudelko = TryCast(oGrid.DataContext, JednoPudelko)
    '        Dim sUri As String = "http://pub.rejestrymedyczne.csioz.gov.pl/"
    '        sUri = sUri & oItem.sDetailsLink   ' "ProduktSzczegoly.aspx?id=22265"
    '        OpenBrowser(sUri)
    '    End If
    'End Sub

    Private Sub uiShowProcedures_Click(sender As Object, e As RoutedEventArgs)
        UCprocedura.ShowProceduryWeb()
    End Sub

    Public Sub WypelnijMenuEAN(oMenu As MenuFlyout)
        Dim sEANy As String = vblib.GetSettingsString("lastEANs")
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


    Private Sub UiLabelEAN_Tapped(sender As Object, e As TappedRoutedEventArgs) Handles uiLabelEAN.Tapped
        ' bo 1809 nie pokazuje FLyout'a , wiec trzeba inną mozliwosc pokazania
        uiLabelEAN.ContextFlyout.ShowAt(uiLabelEAN)
    End Sub

    Private Async Sub uiDodajTenLek_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return

        Dim oItem As vblib.JednoPudelko = TryCast(oMFI.DataContext, vblib.JednoPudelko)
        If oItem Is Nothing Then Return

        For Each oTmp As vblib.JednoPudelko In vblib.globalsy.glZnanePudelka
            If oTmp.sBarcode = oItem.sBarcode Then
                Me.MsgBox("Już to znamy!")
                Return
            End If
        Next

        Me.ProgRingShow(True)

        Await vblib.SzukajInfoLeku.UzupelnijOpakowania(oItem)

        oItem.sCreated = Date.Now.ToString("yyyyMMdd HH:mm")
        ' zapisz dane
        vblib.globalsy.glZnanePudelka.Add(oItem) ' z ewentualną podmianą!
        vblib.globalsy.glZnanePudelka.Save()

        Me.ProgRingShow(False)
    End Sub
End Class
