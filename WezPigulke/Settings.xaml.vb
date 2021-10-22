' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class Settings
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        uiVersion.Text = GetLangString("msgVersion") & " " & Package.Current.Id.Version.Major & "." &
            Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build

        uiIgnoreEmptyLoc.IsOn = GetSettingsBool("ignoreEmptyLocationEvent")
        uiIgnoreNoReminder.IsOn = GetSettingsBool("ignoreNoReminder")
        uiDefSnooze.Value = GetSettingsInt("defaultSnoozeTime", 15)
        uiAllowRemoteSystem.IsOn = GetSettingsBool("allowRemoteSystem")
        uiGenerateToasts.IsOn = GetSettingsBool("generateToasts")
        uiEANitems.Value = GetSettingsInt("maxScannedEANs", 10)
        uiCacheDataHere.IsOn = GetSettingsBool("cacheDataHere")
        uiToastCombo.IsOn = GetSettingsBool("toastListBox")
        uiDailyReschedule.IsOn = GetSettingsBool("dailyReschedule")
        uiShowDebug.IsOn = GetSettingsBool("showDebug")
        If Not TimeSpan.TryParse(GetSettingsString("rescheduleTime", "03:00"), uiRescheduleTime.Time) Then
            uiRescheduleTime.Time = New TimeSpan(3, 0, 0)
        End If
        UiGenerateToasts_Toggled(Nothing, Nothing)
        uiDailyReschedule_Toggled(Nothing, Nothing)
	uiIgnoreLocMask.Text = GetSettingsString("ignoreLocMask", "http|webmeet")

        If IsThisMoje() Then uiToastList.Visibility = Visibility.Visible
    End Sub

    Private Async Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        SetSettingsBool("ignoreEmptyLocationEvent", uiIgnoreEmptyLoc.IsOn)
        SetSettingsBool("ignoreNoReminder", uiIgnoreNoReminder.IsOn)
        SetSettingsInt("defaultSnoozeTime", uiDefSnooze.Value)
        SetSettingsBool("allowRemoteSystem", uiAllowRemoteSystem.IsOn)

        Dim bOldToasts As Boolean = GetSettingsBool("generateToasts")
        SetSettingsBool("generateToasts", uiGenerateToasts.IsOn)

        If uiGenerateToasts.IsOn AndAlso Not bOldToasts Then
            If Await DialogBoxResYN("msgShouldCreateToast") Then uiScheduleToast_Click(Nothing, Nothing)
        End If
        If Not uiGenerateToasts.IsOn AndAlso bOldToasts Then
            If Await DialogBoxResYN("msgShouldRemoveToast") Then App.UsunToasty()
        End If

        SetSettingsInt("maxScannedEANs", uiEANitems.Value)
        SetSettingsBool("cacheDataHere", uiCacheDataHere.IsOn)
        SetSettingsBool("toastListBox", uiToastCombo.IsOn)

        SetSettingsBool("dailyReschedule", uiDailyReschedule.IsOn)
        SetSettingsString("rescheduleTime", uiRescheduleTime.Time.ToString("hh\:mm"))

        SetSettingsBool("showDebug", uiShowDebug.IsOn)
	SetSettingsString("ignoreLocMask", uiIgnoreLocMask.Text)


        Me.Frame.GoBack()
    End Sub


    Private Async Sub uiScheduleToast_Click(sender As Object, e As RoutedEventArgs)
        uiScheduleToast.IsEnabled = False

        ' App.DodajTestoweToasty()

        App.UsunToasty()
        If Not GetSettingsBool("generateToasts") Then
            If Not Await DialogBoxResYN("msgPomimoWylaczenia") Then
                uiScheduleToast.IsEnabled = True
                Return
            End If
        End If
        Await App.ZaplanujToasty()
        Await DialogBoxRes("msgRescheduleDone")
        uiScheduleToast.IsEnabled = True
    End Sub

    Private Sub UiGenerateToasts_Toggled(sender As Object, e As RoutedEventArgs) Handles uiGenerateToasts.Toggled
        Dim bEnabled As Boolean = uiGenerateToasts.IsOn
        uiIgnoreEmptyLoc.IsEnabled = bEnabled
        uiIgnoreNoReminder.IsEnabled = bEnabled
        uiDefSnooze.IsEnabled = bEnabled
        uiToastCombo.IsEnabled = bEnabled
        uiDailyReschedule.IsEnabled = bEnabled
    End Sub

    Private Sub uiDailyReschedule_Toggled(sender As Object, e As RoutedEventArgs) Handles uiDailyReschedule.Toggled
        uiRescheduleTime.IsEnabled = uiDailyReschedule.IsOn
    End Sub

    Private Sub uiToastList_Click(sender As Object, e As RoutedEventArgs)
        Dim sTxt As String = ""

        If Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications().Count > 0 Then
            For Each oItem As Windows.UI.Notifications.ScheduledToastNotification In Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications()
                sTxt = sTxt & "Toast " & oItem.Id & vbCrLf & oItem.DeliveryTime.ToString("dd.MM.yyyy HH:mm") & vbCrLf
            Next
        Else
            sTxt = "nothing scheduled??"
        End If


        DialogBox(sTxt)
    End Sub
End Class
