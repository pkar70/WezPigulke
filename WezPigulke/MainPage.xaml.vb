' v1.1906.19
' 2019.06.11 App:RemoSys: poprawka odsyłania danych
' 2019.06.12 Setting: ignore if Location contains text... (local setting)
' 2019.06.17 App:Dawkowanie2NextTime, poprawka limitu iFor (>= na >)

Public NotInheritable Class MainPage
    Inherits Page

    Dim mbInLoading As Boolean = True

    Private Sub Roznosci()
        ' od 14393, czyli od Aski
        Windows.UI.Notifications.ToastNotificationManager.ConfigureNotificationMirroring(Windows.UI.Notifications.NotificationMirroring.Allowed)
        ' dla RemoteSystem
        Dim oCos = Windows.ApplicationModel.Package.Current.Id.FamilyName
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        'Await DodajWezPigulke()    - plik wrzuc, jak go nie ma w Sounds
        If Not Await App.ZestawyLoad() Then
            If Not Await DialogBoxResYN("msgEmptyRemindersEmpty reminders file or loading error, init it?") Then
                TryCast(Application.Current, App).Exit()
            End If
        End If

        Await App.ZnanePudelkaLoad  ' false bedzie pewnie zwykle - dla nie-Polaków
        Await App.ZnaneSubstancjeLoad

        If GetSettingsBool("generateToasts") Then
            ' reinit nexttime - moze był okres bez aplikacji, i samo się nie zmieniało
            App.Dawkowanie2NextTime(True)
            Await App.ZaplanujToasty()
        End If

        mbInLoading = True  ' zeby nie zapisywal tego co wlasnie odczytal
        uiList.ItemsSource = From c In App.glZestawy Order By c.oNextTime

        Await App.RegisterTriggers()

        Await CrashMessageShow()

        SetSettingsString("resBeforeList", GetLangString("resBeforeList"))
        SetSettingsString("resAfterList", GetLangString("resAfterList"))
        SetSettingsString("resIgnoreList", GetLangString("resIgnoreList"))
        SetSettingsString("resHomeList", GetLangString("resHomeList"))
        SetSettingsString("resBeforeButton", GetLangString("resBeforeButton"))
        SetSettingsString("resAfterButton", GetLangString("resAfterButton"))
        SetSettingsString("resIgnoreButton", GetLangString("resIgnoreButton"))
        SetSettingsString("resHomeButton", GetLangString("resHomeButton"))
        SetSettingsString("resToastTitle", GetLangString("resToastTitle"))
        SetSettingsString("resPillTaken", GetLangString("resPillTaken"))
        SetSettingsString("resPillDelay", GetLangString("resPillDelay"))

        Await Task.Delay(500)   ' musi poczekac - inaczej jest wywolywany Event enable/disable
        mbInLoading = False
    End Sub

    'Private Function DodajWezPigulke() As Task
    '    If Not IsThisMoje() Then Exit Function
    '    ' sprawdz, czy w melodyjkach jest "wezpigulke"
    '    ' jesli nie, to ją dodaj ze swoich zasobów
    'End Function

    Private Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)
        mbInLoading = True
        Me.Frame.Navigate(GetType(AddZestaw))
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As JedenZestaw = TryCast(sender, MenuFlyoutItem).DataContext
        If oItem Is Nothing Then Return
        mbInLoading = True
        Me.Frame.Navigate(GetType(AddZestaw), oItem.sId)
    End Sub


    Private Sub uiSettings_Click(sender As Object, e As RoutedEventArgs)
        mbInLoading = True
        Me.Frame.Navigate(GetType(Settings))
    End Sub

    'Private Async Sub uiTaken_Click(sender As Object, e As RoutedEventArgs)
    '    Dim oItem As JedenZestaw = TryCast(sender, Grid).DataContext
    '    If oItem Is Nothing Then Return
    '    Await App.WzialemPigulke(oItem.sId, oItem.sNextOrgTime)
    'End Sub

    Private Sub uiLekInfo_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(SzukajInfoLeku))
    End Sub

    Private Sub uiSearchApteki_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(SzukajAptekLeku))
    End Sub

    Private Sub uiLibrary_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim bPL As Boolean = False
        Try
            If GetLangString("_lang") = "PL" Then bPL = True
        Catch
        End Try

        If Not bPL AndAlso Not GetSettingsBool("warningPLshown") Then
            DialogBox("This functionality is designed to work in Poland only")
            SetSettingsBool("warningPLshown", True)
        End If

    End Sub

    Private Sub uiMojeLeki_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(MojeLeki))
    End Sub

    Private Async Sub uiEnable_Checked(sender As Object, e As RoutedEventArgs)
        If mbInLoading Then Return
        ' tu mozna usunac Toasty zwiazane z tym zestawem, albo je dodac...
        ' tak jakby zbyt czesto sie to dzialo...
        ' Await App.ZestawySave(True)
        ' Wersja 2: nie jest TwoWay, a OneWay
        ' - czyli: przełącz
        Dim oItem As JedenZestaw = TryCast(sender, CheckBox).DataContext
        If oItem Is Nothing Then Return
        oItem.bEnabled = Not oItem.bEnabled
        mbInLoading = True  ' zeby nie zapisywal tego co wlasnie odczytal
        uiList.ItemsSource = From c In App.glZestawy Order By c.oNextTime
        Await App.ZestawySave(True)
        Await Task.Delay(500)
        mbInLoading = False
    End Sub

    Private Sub uiDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As JedenZestaw = TryCast(sender, MenuFlyoutItem).DataContext
        If oItem Is Nothing Then Return

        Dim sTxt As String = "Details" & vbCrLf
        sTxt = sTxt & "sId=" & oItem.sId & vbCrLf
        sTxt = sTxt & "sNazwaZestawu=" & oItem.sNazwaZestawu & vbCrLf
        sTxt = sTxt & "sTakeTimes=" & oItem.sTakeTimes & vbCrLf
        sTxt = sTxt & "sNextOrgTime=" & oItem.sNextOrgTime & vbCrLf
        sTxt = sTxt & "iDelayMins=" & oItem.iDelayMins & vbCrLf
        sTxt = sTxt & "bEnabled=" & oItem.bEnabled & vbCrLf
        'sTxt = sTxt & "bEnabled=" & oItem.bEnabled & vbCrLf
        sTxt = sTxt & "oNextOrgTime=" & oItem.oNextOrgTime.ToString("yyyy.MM.dd HH:mm:ss") & vbCrLf
        sTxt = sTxt & "oNextTime=" & oItem.oNextTime.ToString("yyyy.MM.dd HH:mm:ss") & vbCrLf
        sTxt = sTxt & "sDisplayTime=" & oItem.sDisplayTime & vbCrLf
        sTxt = sTxt & "sDisplayOrgTime=" & oItem.sDisplayOrgTime & vbCrLf

        DialogBox(sTxt)
    End Sub
End Class
