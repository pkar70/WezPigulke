Imports pkar.UI.Extensions
Imports pkar.UI.Configs

Public NotInheritable Class Settings
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

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
            If Await Me.DialogBoxYNAsync("msgShouldCreateToast") Then uiScheduleToast_Click(Nothing, Nothing)
        End If
        If Not uiGenerateToasts.IsOn AndAlso bOldToasts Then
            If Await Me.DialogBoxYNAsync("msgShouldRemoveToast") Then App.UsunToasty() ' Toasts.RemoveScheduledToasts()  'App.UsunToasty()
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

        If uiCheckWycofania.IsOn AndAlso Not bOldToasts Then
            Await App.TriggerWycofaniaReschedule
        End If
        If Not uiGenerateToasts.IsOn Then
            App.UnregisterTriggers() '("WezPigulkeWycofaniaTrigger")
        End If
        uiToastForAll.SetSettingsBool

        Me.Frame.GoBack()
    End Sub


    Private Async Sub uiScheduleToast_Click(sender As Object, e As RoutedEventArgs)
        uiScheduleToast.IsEnabled = False

        ' App.DodajTestoweToasty()

        App.UsunToasty()
        If Not vblib.GetSettingsBool("generateToasts") Then
            If Not Await Me.DialogBoxYNAsync("msgPomimoWylaczenia") Then
                uiScheduleToast.IsEnabled = True
                Return
            End If
        End If
        Await App.ZaplanujToasty()
        Me.MsgBox("msgRescheduleDone")
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

    Private Async Sub uiSendEmail_Click(sender As Object, e As RoutedEventArgs)
        Dim oMsg As New Windows.ApplicationModel.Email.EmailMessage()
        oMsg.Subject = "dane z pigularza"

        oMsg.Attachments.Add(New Email.EmailAttachment())

        Dim sTxt As String = "Załączam dane z pigularza" & vbCrLf
        'sTxt &= Await ReadRoamFile("zestawy.xml") & vbCrLf
        'sTxt &= Await ReadRoamFile("pudelka.xml") & vbCrLf
        'sTxt &= Await ReadRoamFile("substancje.xml") & vbCrLf
        oMsg.Body = sTxt

        Await AddAttachment(oMsg, "zestawy.xml")
        Await AddAttachment(oMsg, "pudelka.xml")
        Await AddAttachment(oMsg, "substancje.xml")

        Await Email.EmailManager.ShowComposeNewEmailAsync(oMsg)

    End Sub

    Private Async Function ReadRoamFile(fname As String) As Task(Of String)

        Dim ret As String = vbCrLf & $"plik {fname}: "

        Dim oFile As Windows.Storage.StorageFile = Await App.GetRoamingFile(fname, False)

        If oFile Is Nothing Then Return ret & "nie istnieje"


        ret &= vbCrLf

        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Dim rdr As New StreamReader(oStream)

        ret &= rdr.ReadToEnd

        'rdr.Close()
        rdr.Dispose()
        oStream.Dispose()

        Return ret
    End Function

    Private Async Function AddAttachment(oMsg As Windows.ApplicationModel.Email.EmailMessage, fname As String) As Task
        Dim oFile As Windows.Storage.StorageFile = Await App.GetRoamingFile(fname, False)

        If oFile Is Nothing Then Return

        oMsg.Attachments.Add(New Email.EmailAttachment(fname, oFile))

    End Function

End Class
