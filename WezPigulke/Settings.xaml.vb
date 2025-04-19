Imports pkar.UI.Extensions
Imports pkar.UI.Configs

Public NotInheritable Class Settings
    Inherits Page

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        uiVersion.ShowAppVers(False)

        uiIgnoreEmptyLoc.GetSettingsBool("ignoreEmptyLocationEvent")
        uiIgnoreNoReminder.GetSettingsBool("ignoreNoReminder")
        uiDefSnooze.Value = vblib.GetSettingsInt("defaultSnoozeTime", 15)
        uiAllowRemoteSystem.GetSettingsBool("allowRemoteSystem")
        uiGenerateToasts.GetSettingsBool("generateToasts")
        uiEANitems.Value = vblib.GetSettingsInt("maxScannedEANs", 10)
        uiCacheDataHere.GetSettingsBool("cacheDataHere")
        uiToastCombo.GetSettingsBool("toastListBox")
        uiDailyReschedule.GetSettingsBool("dailyReschedule")
        uiShowDebug.GetSettingsBool("showDebug")
        If Not TimeSpan.TryParse(vblib.GetSettingsString("rescheduleTime", "03:00"), uiRescheduleTime.Time) Then
            uiRescheduleTime.Time = New TimeSpan(3, 0, 0)
        End If
        'UiGenerateToasts_Toggled(Nothing, Nothing)
        uiDailyReschedule_Toggled(Nothing, Nothing)
        uiIgnoreLocMask.Text = vblib.GetSettingsString("ignoreLocMask", "http|webmeet")
        uiCheckWycofania.GetSettingsBool
        uiToastForAll.GetSettingsBool

        If IsThisMoje() Then uiToastList.Visibility = Visibility.Visible
    End Sub

    Private Async Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        uiIgnoreEmptyLoc.SetSettingsBool("ignoreEmptyLocationEvent")
        uiIgnoreNoReminder.SetSettingsBool("ignoreNoReminder")
        uiDefSnooze.SetSettingsInt("defaultSnoozeTime")
        uiAllowRemoteSystem.SetSettingsBool("allowRemoteSystem")

        Dim bOldToasts As Boolean = vblib.GetSettingsBool("generateToasts")
        uiGenerateToasts.SetSettingsBool("generateToasts")

        If uiGenerateToasts.IsOn AndAlso Not bOldToasts Then
            If Await Me.DialogBoxYNAsync("res:msgShouldCreateToast") Then uiScheduleToast_Click(Nothing, Nothing)
        End If
        If Not uiGenerateToasts.IsOn AndAlso bOldToasts Then
            If Await Me.DialogBoxYNAsync("res:msgShouldRemoveToast") Then App.UsunToasty() ' Toasts.RemoveScheduledToasts()  'App.UsunToasty()
        End If

        uiEANitems.SetSettingsInt("maxScannedEANs")
        uiCacheDataHere.SetSettingsBool("cacheDataHere")
        uiToastCombo.SetSettingsBool("toastListBox")

        uiDailyReschedule.SetSettingsBool("dailyReschedule")
        vblib.SetSettingsString("rescheduleTime", uiRescheduleTime.Time.ToString("hh\:mm"))

        uiShowDebug.SetSettingsBool("showDebug")
        uiIgnoreLocMask.SetSettingsString("ignoreLocMask")

        bOldToasts = vblib.GetSettingsBool("uiCheckWycofania")
        uiCheckWycofania.SetSettingsBool
        uiToastForAll.SetSettingsBool

        If uiCheckWycofania.IsOn Then

            If vblib.globalsy.glWycofaniaGIF Is Nothing Then
                App.InitLoadDecyzje()
                If vblib.globalsy.glWycofaniaGIF.Count < 10 Then
                    If Await Me.DialogBoxYNAsync("Wczytać dotychczasowe decyzje?") Then
                        Me.ProgRingShow(True)
                        Dim guard As Integer = 20
                        While guard > 0
                            Dim countPre As Integer = vblib.globalsy.glWycofaniaGIF.Count
                            Await vblib.globalsy.glWycofaniaGIF.ImportNewDecyzje(100)
                            If countPre + 2 < vblib.globalsy.glWycofaniaGIF.Count Then Exit While
                            guard -= 1
                        End While
                        Me.ProgRingShow(False)
                    End If
                End If
            End If

        End If



        ' rescheduling będzie w MainPage.Page_Loaded
        'If uiCheckWycofania.IsOn <> bOldToasts Then
        '    Await App.TriggerWycofaniaReschedule()
        'End If

        'Await App.TriggerNocnyReschedule()

        'If Not uiGenerateToasts.IsOn Then
        '    App.UnregisterTriggers("WezPigulkeRescheduleToast") '("WezPigulkeWycofaniaTrigger")
        'End If

        Me.Frame.GoBack()
    End Sub


    Private Async Sub uiScheduleToast_Click(sender As Object, e As RoutedEventArgs)
        uiScheduleToast.IsEnabled = False

        ' App.DodajTestoweToasty()

        App.UsunToasty()
        If Not vblib.GetSettingsBool("generateToasts") Then
            If Not Await Me.DialogBoxYNAsync("res:msgPomimoWylaczenia") Then
                uiScheduleToast.IsEnabled = True
                Return
            End If
        End If
        Await App.ZaplanujToasty()
        Me.MsgBox("res:msgRescheduleDone")
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

        Me.MsgBox(sTxt)
    End Sub


End Class
