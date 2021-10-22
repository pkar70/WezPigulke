''' <summary>
''' Provides application-specific behavior to supplement the default Application class.
''' </summary>
Partial NotInheritable Class App
    Inherits Application

    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>
    Protected Overrides Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
        Dim rootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If rootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = New Frame()

            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

            If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
                ' TODO: Load state from previously suspended application
            End If
            ' Place the frame in the current Window
            Window.Current.Content = rootFrame
        End If

        If e.PrelaunchActivated = False Then
            If rootFrame.Content Is Nothing Then
                ' When the navigation stack isn't restored navigate to the first page,
                ' configuring the new page by passing required information as a navigation
                ' parameter
                rootFrame.Navigate(GetType(MainPage), e.Arguments)
            End If

            ' Ensure the current window is active
            Window.Current.Activate()
        End If

    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub

    Public Shared Async Function GetRoamingFile(sName As String, bCreate As Boolean) As Task(Of Windows.Storage.StorageFile)
        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.RoamingFolder
        If oFold Is Nothing Then
            Await DialogBoxRes("errNoRoamFolder")
            Return Nothing
        End If

        Dim bErr As Boolean = False
        Dim oFile As Windows.Storage.StorageFile = Nothing
        Try
            If bCreate Then
                oFile = Await oFold.CreateFileAsync(sName, Windows.Storage.CreationCollisionOption.ReplaceExisting)
            Else
                oFile = Await oFold.TryGetItemAsync(sName)
            End If
        Catch ex As Exception
            bErr = True
        End Try
        If bErr Then
            Return Nothing
        End If

        Return oFile
    End Function


    'Public Shared gsResBeforeButton As String
    'Public Shared gsResAfterButton As String
    'Public Shared gsResIgnoreButton As String
    'Public Shared gsResHomeButton As String
    'Public Shared gsResBeforeList As String
    'Public Shared gsResAfterList As String
    'Public Shared gsResIgnoreList As String
    'Public Shared gsResHomeList As String
    'Public Shared gsResToastTitle As String

    Public Shared glZestawy As Collection(Of JedenZestaw)
    Const FILENAME_ZESTAWY As String = "zestawy.xml"
    Private Shared bZestawyDirty As Boolean

    Private Shared Async Function ZestawyFileLoad() As Task(Of Boolean)
        App.glZestawy = New Collection(Of JedenZestaw)
        Dim oFile As Windows.Storage.StorageFile = Await App.GetRoamingFile(FILENAME_ZESTAWY, False)
        If oFile Is Nothing Then Return True    ' czyli jest OK

        Dim oSer As Xml.Serialization.XmlSerializer =
                New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenZestaw)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Dim bError As Boolean = False
        Try
            glZestawy = TryCast(oSer.Deserialize(oStream), Collection(Of JedenZestaw))
        Catch ex As Exception
            bError = True
        End Try
        oStream.Dispose()
        oStream = Nothing
        oSer = Nothing
        oFile = Nothing
        bZestawyDirty = False

        Return Not bError

    End Function


    Public Shared Async Function ZestawyLoad() As Task(Of Boolean)
        If Not Await App.ZestawyFileLoad Then Return False

        Dim bMoje As Boolean = IsThisMoje()
        ' wylicz oNextTime, sNextTime, iMinsToTake 
        For Each oItem As JedenZestaw In App.glZestawy
            'oItem.iMinsToTake = oItem.oNextTime - Date.Now
            Dim oDate As DateTime
            If Not Date.TryParseExact(oItem.sNextOrgTime, "yyyyMMddHHmm", Nothing, Globalization.DateTimeStyles.None, oDate) Then
                oItem.sDisplayTime = "<??>"
                'oItem.oNextOrgTime = New Date(9001, 12, 31)   ' aby bylo na koncu sortowania
                'oItem.oNextTime = New Date(9001, 12, 31)   ' aby bylo na koncu sortowania
            Else
                oItem.oNextOrgTime = oDate
                oItem.oNextTime = oDate.AddMinutes(oItem.iDelayMins)
                oItem.sDisplayTime = oItem.oNextTime.ToString("HH:mm")

                If oItem.iDelayMins <> 0 Then
                    oItem.sDisplayOrgTime = "(org: " & oItem.oNextOrgTime.ToString("HH:mm") & ")"
                End If
            End If

            oItem.bIsThisMoje = bMoje
        Next

        Return True
    End Function


    Public Shared Async Function ZestawySave(bForce As Boolean) As Task

        If Not bZestawyDirty AndAlso Not bForce Then Return

        Dim oFile As Windows.Storage.StorageFile = Await App.GetRoamingFile(FILENAME_ZESTAWY, True)
        If oFile Is Nothing Then Exit Function

        Dim oSer As Xml.Serialization.XmlSerializer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenZestaw)))
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        oSer.Serialize(oStream, glZestawy)
        oStream.Dispose()   ' == fclose
        oStream = Nothing
        oSer = Nothing
        oFile = Nothing
        bZestawyDirty = False
    End Function

    Public Shared Sub ZestawyAdd(oNew As JedenZestaw)
        Dim bToEdit As Boolean = False

        For Each oItem As JedenZestaw In App.glZestawy
            If oItem.sId = oNew.sId Then
                oItem.sNazwaZestawu = oNew.sNazwaZestawu
                oItem.sTakeTimes = oNew.sTakeTimes
                oItem.sMelodyjka = oNew.sMelodyjka
                oItem.sNextOrgTime = ""
                bToEdit = True
                Exit For
            End If
        Next

        If Not bToEdit Then
            App.glZestawy.Add(oNew)
        End If

        bZestawyDirty = True

        App.Dawkowanie2NextTime(False)


    End Sub

    Public Shared Sub Dawkowanie2NextTime(oItem As JedenZestaw, bReset As Boolean)
        ' wylicza oItem.oNextTime (=oItem.oNextOrgTime), sNext*Time, iDelay=0

        Dim aArr As String() = oItem.sTakeTimes.Split("|")
        Dim bAllSame As Boolean = True
        For iFor As Integer = 1 To 6
            If iFor > aArr.GetUpperBound(0) Then Exit For
            If aArr(iFor) <> aArr(0) Then
                bAllSame = False
                Exit For
            End If
        Next


        Dim oDateAlmostNow As DateTimeOffset

        If bReset OrElse oItem.oNextTime = Nothing OrElse oItem.oNextTime.Year > 9000 Then
            oDateAlmostNow = Date.Now.AddMinutes(5) ' 5 minut pozniej - zeby nie trafilo w tą samą minutę :)  ' AddMinutes(-oItem.iDelayMins)   ' tak zeby opoznienie uwzglednic
        Else
            oDateAlmostNow = oItem.oNextOrgTime.AddMinutes(5)   ' niby to jest czas w ktorym mielismy zjeść (pierwotny, bez opóźnień)
        End If
        oItem.iDelayMins = 0

        ' z danego rządka (albo z rządka 0, gdy bAllSame)
        ' Dim bFirst As Boolean = True
        Dim iDTyg As Integer = oDateAlmostNow.DayOfWeek   ' niedziela = 0
        If bAllSame Then iDTyg = 0

        Dim iZaDni As Integer = 0
        Do
            Select Case aArr(iDTyg).Substring(0, 1)
                Case 0  ' w ogóle nie
                Case 1  ' jedna godzina
                    If iZaDni = 0 Then
                        If oDateAlmostNow.Hour > aArr(iDTyg).Substring(2, 2) Then Exit Select
                        If oDateAlmostNow.Hour = aArr(iDTyg).Substring(2, 2) AndAlso
                                   oDateAlmostNow.Minute > aArr(iDTyg).Substring(4, 2) Then Exit Select
                    End If
                    oItem.oNextOrgTime =
                                New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day,
                                         aArr(iDTyg).Substring(2, 2), aArr(iDTyg).Substring(4, 2), 0).AddDays(iZaDni)
                    Exit Do
                Case 2  ' 9 i 21
                    If iZaDni = 0 Then
                        If oDateAlmostNow.Hour > 21 Then Exit Select
                        If oDateAlmostNow.Hour = 21 AndAlso oDateAlmostNow.Minute > 0 Then Exit Select
                        oItem.oNextOrgTime =
                                    New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 21, 0, 0).AddDays(iZaDni)
                        If oDateAlmostNow.Hour > 9 Then Exit Do
                        If oDateAlmostNow.Hour = 9 AndAlso oDateAlmostNow.Minute > 0 Then Exit Do
                    End If
                    oItem.oNextOrgTime =
                                New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 9, 0, 0).AddDays(iZaDni)
                    Exit Do
                Case 3  ' 8, 16, 23
                    If iZaDni = 0 Then
                        If oDateAlmostNow.Hour > 23 Then Exit Select
                        If oDateAlmostNow.Hour = 23 AndAlso oDateAlmostNow.Minute > 0 Then Exit Select
                        oItem.oNextOrgTime =
                                    New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 23, 0, 0).AddDays(iZaDni)
                        If oDateAlmostNow.Hour > 16 Then Exit Do
                        If oDateAlmostNow.Hour = 16 AndAlso oDateAlmostNow.Minute > 0 Then Exit Do
                        oItem.oNextOrgTime =
                                    New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 16, 0, 0).AddDays(iZaDni)
                        If oDateAlmostNow.Hour > 8 Then Exit Do
                        If oDateAlmostNow.Hour = 8 AndAlso Date.Now.Minute > 0 Then Exit Do
                    End If
                    oItem.oNextOrgTime =
                                New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, 8, 0, 0).AddDays(iZaDni)
                    Exit Do
            End Select

            iZaDni += 1
            ' jeśli juz jest po ostatnim terminie w dniu, to następny rządek (lub rządek 0)
            If Not bAllSame Then
                iDTyg += 1
                If iDTyg > 6 Then iDTyg = 0
            End If
            ' bFirst = False
        Loop

        oItem.sNextOrgTime = oItem.oNextOrgTime.ToString("yyyyMMddHHmm")
        oItem.oNextTime = oItem.oNextOrgTime
        oItem.iDelayMins = 0
        oItem.sDisplayOrgTime = ""
        oItem.sDisplayTime = oItem.oNextTime.ToString("HH:mm")

        bZestawyDirty = True
    End Sub

    Public Shared Sub Dawkowanie2NextTime(bReset As Boolean)
        For Each oItem As JedenZestaw In glZestawy

            If bReset OrElse oItem.oNextTime.Year > 9000 Then
                ' wylicz next time
                Dawkowanie2NextTime(oItem, bReset)
            End If
        Next
    End Sub


    'Public Shared Function PlayAlarm() As Task
    '    ' tu wlacz granie w petli
    '    Dim oMediaPlayer As Windows.Media.Playback.MediaPlayer =
    '                        New Windows.Media.Playback.MediaPlayer()
    '    oMediaPlayer.IsLoopingEnabled = True
    '    oMediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(New Uri("ms-appx:///Assets/TimerAlarm.mp3"))
    '    oMediaPlayer.Play()
    'End Function

#Region "triggers"

    Public Shared Sub UnregisterTriggers()
        For Each oTask As KeyValuePair(Of Guid, Background.IBackgroundTaskRegistration) In Background.BackgroundTaskRegistration.AllTasks
            If oTask.Value.Name.StartsWith("WezPigulke") Then oTask.Value.Unregister(True)
        Next
    End Sub

    Public Shared Async Function TriggerNocnyReschedule() As Task
        For Each oTask As KeyValuePair(Of Guid, Background.IBackgroundTaskRegistration) In Background.BackgroundTaskRegistration.AllTasks
            If oTask.Value.Name = "WezPigulkeRescheduleToast" Then oTask.Value.Unregister(True)
        Next

        If Not GetSettingsBool("dailyReschedule") Then Return

        Dim oBAS As Background.BackgroundAccessStatus
        oBAS = Await Background.BackgroundExecutionManager.RequestAccessAsync()

        Dim builder As Background.BackgroundTaskBuilder = New Background.BackgroundTaskBuilder
        Dim oRet As Background.BackgroundTaskRegistration

        If oBAS = Background.BackgroundAccessStatus.AlwaysAllowed Or oBAS = Background.BackgroundAccessStatus.AllowedSubjectToSystemPolicy Then
            Dim oTS As TimeSpan
            If Not TimeSpan.TryParse(GetSettingsString("rescheduleTime", "03:00"), oTS) Then
                oTS = New TimeSpan(3, 0, 0)
            End If

            Dim oDate As Date = New Date(Date.Now.Year, Date.Now.Month, Date.Now.Day, oTS.Hours, oTS.Minutes, 0)
            If oDate < Date.Now Then oDate = oDate.AddDays(1)
            builder.SetTrigger(New Background.TimeTrigger((oDate - Date.Now).TotalMinutes, True))
            builder.Name = "WezPigulkeRescheduleToast"
            oRet = builder.Register()
        End If

    End Function

    Public Shared Async Function RegisterTriggers() As Task(Of Boolean)
        UnregisterTriggers()

        Dim oBAS As Background.BackgroundAccessStatus
        oBAS = Await Background.BackgroundExecutionManager.RequestAccessAsync()


        ' https://docs.microsoft.com/en-us/windows/uwp/launch-resume/create-And-register-an-inproc-background-task
        Dim builder As Background.BackgroundTaskBuilder = New Background.BackgroundTaskBuilder
        Dim oRet As Background.BackgroundTaskRegistration

        If oBAS = Background.BackgroundAccessStatus.AlwaysAllowed Or oBAS = Background.BackgroundAccessStatus.AllowedSubjectToSystemPolicy Then
            builder.SetTrigger(New Background.ToastNotificationActionTrigger)
            builder.Name = "WezPigulkeToast"
            oRet = builder.Register()
            builder.SetTrigger(New Background.AppointmentStoreNotificationTrigger)
            builder.Name = "WezPigulkeCalUpdate"

            Await TriggerNocnyReschedule()
            Return True
        Else
            Return False
        End If

    End Function

    Dim moTimerDeferal As Background.BackgroundTaskDeferral

    Protected Overrides Async Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)

        moTimerDeferal = args.TaskInstance.GetDeferral()

        If Not Await CalledFromBackground(args.TaskInstance) Then
            moTimerDeferal.Complete()   ' gdy to byl RemoteSystem, to nie dereferuj
        End If

        'If rootFrame IsNot Nothing Then
        '    MakeToast("onbackground rootframe", "onbackground rootframe", "onbackground rootframe")
        '    Dim oMain As MainPage = TryCast(rootFrame.Content, MainPage)
        '    If oMain IsNot Nothing Then Await oMain.CalledFromBackground(args.TaskInstance)
        'Else
        '    MakeToast("onbackground NULL", "onbackground NULL", "onbackground NULL")
        'End If

    End Sub

    Private Shared Async Function AkcjeSnoozeWedleKalendarza(oItem As JedenZestaw, sActionId As String, bCanModifyTime As Boolean) As Task(Of String)
        ' uwaga: modyfikuje oItem.iDelayMins - przesuwając przed termin (na reminder) albo na po

        If oItem.oNextTime.Year > 9000 Then Return ""

        Dim oStore As Appointments.AppointmentStore = Nothing
        Dim oCalendars As IReadOnlyList(Of Appointments.Appointment)

        Try
            oStore = Await Appointments.AppointmentManager.RequestStoreAsync(Appointments.AppointmentStoreAccessType.AllCalendarsReadOnly)

            'Dim oTask = Appointments.AppointmentManager.RequestStoreAsync(Appointments.AppointmentStoreAccessType.AllCalendarsReadOnly).AsTask.AsAsyncOperation

            'Await Task.Delay(1000)
            'If oTask.Status = AsyncStatus.Completed Then
            '    oStore = oTask.GetResults
            'Else
            '    oTask.Cancel()
            '    CrashMessageAdd("Timeout getting calendar", "")
            '    Return ""
            'End If

            If oStore Is Nothing Then Return ""

            Dim oCalOpt As Appointments.FindAppointmentsOptions = New Appointments.FindAppointmentsOptions
            oCalOpt.IncludeHidden = True
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.AllDay)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Location)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Reminder)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.StartTime)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Duration)
            oCalOpt.FetchProperties.Add(Appointments.AppointmentProperties.Subject) ' tylko dla celów debug
            oCalOpt.MaxCount = 20
            oCalendars = Await oStore.FindAppointmentsAsync(Date.Now, TimeSpan.FromDays(7), oCalOpt)
            If oCalendars Is Nothing Then Return ""
        Catch ex As Exception
            CrashMessageAdd("@AkcjeSnoozeWedleKalendarza part 1", ex.Message)
            Return ""
        End Try

        Dim oSeriaStart As DateTimeOffset
        Dim oSeriaStop As DateTimeOffset
        Dim oSeriaStartRemind As DateTimeOffset
        Dim oSeriaStopRemind As DateTimeOffset

        Dim oApp As Appointments.Appointment
        Dim bFirst As Boolean = True

        ' Dim oTakeDate As DateTimeOffset = oItem.oNextTime   ' z przesunieciem o Snooze

        Try
            For Each oApp In oCalendars
                ' pomijamy calodzienne, krotsze niz 15 minut i dluzsze niz 12 godzin
                If oApp.AllDay Then Continue For
                If oApp.Duration < TimeSpan.FromMinutes(15) OrElse oApp.Duration > TimeSpan.FromHours(12) Then
                    Continue For
                End If
                If oApp.Location = "" AndAlso GetSettingsBool("ignoreEmptyLocationEvent") Then Continue For
		
		Dim aIgnore As String() = GetSettingsString("ignoreLocMask", "http|webmeet").Split("|")
		Dim bIgnoreLoc As Boolean = False
		For Each sIgnoreLoc As String In aIgnore 
			If oApp.Location.ToLower().Contains(sIgnoreLoc) Then bIgnoreLoc = True
		Next
		If bIgnoreLoc Then Continue For

                Dim oAppStart As DateTimeOffset
                Dim oAppStartRemind As DateTimeOffset
                Dim oAppStop As DateTimeOffset
                Dim oAppStopRemind As DateTimeOffset

                oAppStart = oApp.StartTime
                oAppStop = oApp.StartTime + oApp.Duration

                ' uwzglednienie czasu reminder (przed i po event)
                If oApp.Reminder Is Nothing OrElse Not oApp.Reminder.HasValue Then
                    If GetSettingsBool("ignoreNoReminder") Then Continue For

                    oAppStartRemind = oAppStart
                    oAppStopRemind = oAppStop
                Else
                    oAppStartRemind = oAppStart - oApp.Reminder.Value
                    oAppStopRemind = oAppStop + oApp.Reminder.Value
                End If

                ' zdarzenie PRZED pigułką
                If oAppStopRemind < oItem.oNextTime Then Continue For

                ' aktualizacja dat/godzin serii
                If bFirst Then
                    oSeriaStart = oApp.StartTime
                    oSeriaStartRemind = oAppStartRemind
                    oSeriaStop = oAppStop
                    oSeriaStopRemind = oAppStopRemind
                    bFirst = False
                Else
                    If oSeriaStopRemind < oAppStartRemind Then Exit For

                    ' bo moze byc pozniejsze zdarzenie z dluzszym Reminder, ktory w efekcie wypada wczesniej?
                    If oSeriaStart > oAppStart Then oSeriaStart = oApp.StartTime        ' MIN
                    If oSeriaStartRemind > oAppStartRemind Then oSeriaStartRemind = oAppStartRemind ' MIN
                    If oSeriaStop < oAppStop Then oSeriaStop = oAppStop     ' MAX
                    If oSeriaStopRemind < oAppStopRemind Then oSeriaStopRemind = oAppStopRemind ' MAX
                End If

                ' If oAppStartRemind > oSeriaStopRemind Then Exit For ' usuwam 20190522, bo jest juz w IF(!first), w First zawsze FALSE

            Next

            If bFirst Then Return ""
        Catch ex As Exception
            CrashMessageAdd("@AkcjeSnoozeWedleKalendarza For/Next", ex.Message)
        End Try

        ' mamy juz daty poustawiane, teraz dodaj akcje
        If oItem.oNextTime < oSeriaStartRemind Then Return ""

        ' mamy oNextOrgTime (=sTakeOrgTime), oraz iDelayMins i oNextTime (=oNextOrgTime+iDelay)
        ' zmieniamy iDelayMins/oNextTime tak by Toast sie w dobrym momencie pojawil

        ' przesun Toast przed Reminder wyjazdowy
        If bCanModifyTime Then
            ' nie moze zmieniac, gdy to jest snooze w minutach!
            oItem.iDelayMins = -oItem.oNextOrgTime.Subtract(oSeriaStartRemind).TotalMinutes + oItem.iDelayMins
        End If

        ' przelicz na wszelki wypadek pozostale (wyliczane)
        oItem.oNextTime = oItem.oNextOrgTime.AddMinutes(oItem.iDelayMins)
        If oItem.oNextTime < Date.Now.AddMinutes(5) Then
            oItem.oNextTime = Date.Now.AddMinutes(5)
            oItem.iDelayMins = (oItem.oNextTime - oItem.oNextOrgTime).TotalMinutes + 5
        End If

        bZestawyDirty = True
        If oItem.iDelayMins <> 0 Then
            oItem.sDisplayTime = oItem.oNextTime.ToString("HH:mm")
            oItem.sDisplayOrgTime = "(org: " & oItem.oNextOrgTime.ToString("HH:mm") & ")"
        Else
            oItem.sDisplayOrgTime = ""
            oItem.sDisplayTime = oItem.oNextTime.ToString("HH:mm")
        End If

        ' skoro byla zmiana (iDelayMins), to zapisanie zestawow bedzie potrzebne
        ' Await ZestawySave() - zeby nie zapisywac seryjnie - zrob to 'pietro wyzej' (raz dla wszystkich)

        Dim sActions As String = ""
        Dim sActionPrefix As String
        If GetSettingsBool("toastListBox") Then
            sActionPrefix = "<selection id="""
        Else
            sActionPrefix = "<action activationType=""background"" arguments=""DELAY" & sActionId
        End If
        Dim iMin As Integer


        If oItem.oNextTime < oSeriaStart.AddMinutes(-5) Then
            ' AKCJA: przypomnij tuz przed event
            iMin = (oSeriaStart - oItem.oNextTime).TotalMinutes - 5
            If iMin > 0 AndAlso oSeriaStart.AddMinutes(-5) > Date.Now Then
                If GetSettingsBool("toastListBox") Then
                    sActions = sActions & sActionPrefix & iMin & """ content=""" & GetSettingsString("resBeforeList") & """ />"
                Else
                    sActions = sActions & sActionPrefix & iMin & """ content=""" & GetSettingsString("resBeforeButton") & """/>"
                End If
            End If
        End If
        If oItem.oNextTime < oSeriaStop Then
            ' AKCJA: przypominij po stoptime
            iMin = (oSeriaStop - oItem.oNextTime).TotalMinutes + 5
            If iMin > 0 AndAlso oSeriaStop > Date.Now Then
                If GetSettingsBool("toastListBox") Then
                    sActions = sActions & sActionPrefix & iMin & """ content=""" & GetSettingsString("resAfterList") & """ />"
                Else
                    sActions = sActions & sActionPrefix & iMin & """ content=""" & GetSettingsString("resAfterButton") & """/>"
                End If
            End If
        End If

        ' AKCJA: przypominij po stoptimereminder
        iMin = (oSeriaStopRemind - oItem.oNextTime).TotalMinutes + 5
        If iMin > 0 Then
            If GetSettingsBool("toastListBox") Then
                sActions = sActions & sActionPrefix & iMin & """ content=""" & GetSettingsString("resHomeList") & """ />"
            Else
                sActions = sActions & sActionPrefix & iMin & """ content=""" & GetSettingsString("resHomeButton") & """/>"
            End If
        End If

        Return sActions

        ' uwaga:
        ' https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/adaptive-interactive-toasts
        ' You can only have up to 5 buttons
        ' button moze miec ikonke, 16x16 - i to moze sie przydac...
        ' imageUri="Assets/ToastButtonIcons/Dismiss.png"
        ' obrazek kalendarza i kropka przed, lub kropka po?



    End Function

    Public Shared Async Function AkcjeSnoozeList(oItem As JedenZestaw, sActionId As String, bUseCalendar As Boolean, bCanModifyTime As Boolean) As Task(Of String)
        Dim sActions As String


        sActions = "<input id=""delayTime"" type=""selection"" defaultInput=""x15"">"
        If bUseCalendar Then sActions = sActions & Await AkcjeSnoozeWedleKalendarza(oItem, sActionId, bCanModifyTime)

        Dim sActionPrefix As String = "<selection id="

        sActions = sActions &
            sActionPrefix & "x15"" content=""15 mins""/>" &
            sActionPrefix & "x30"" content=""30 mins""/>" &
            sActionPrefix & "x60"" content=""60 mins""/>"

        sActions = sActions & "</input>"

        Return sActions
    End Function

    Private Shared Sub DodajTestowyToast(bList As Boolean, oDate As Date)
        Dim sXml As String = "<visual><binding template=""ToastGeneric"">" &
            "<text>Pigułkowe larum:</text><text>Testowy toast (@ " & Date.Now.ToString("MMdd.HH:mm:ss") & "</text></binding></visual>" &
            "<actions>"

        Dim sActionId As String = "DELAYtestowy@timeorg@"
        Dim sActionPrefix As String
        Dim iMin As Integer = 0

        If bList Then
            sActionPrefix = "<selection id="""
            sXml = sXml & "<input id=""delayTime"" type=""selection"" defaultInput=""actionId15"">"
            sXml = sXml & sActionPrefix & "before"" content=""before event"" />"
            sXml = sXml & sActionPrefix & "after"" content=""after event"" />"
            sXml = sXml & sActionPrefix & "home"" content=""back at home"" />"
            sXml = sXml & sActionPrefix & "15"" content=""15 mins""/>"
            ' sXml = sXml & sActionPrefix & "30"" content=""30 mins""/>"
            sXml = sXml & sActionPrefix & "60"" content=""60 mins""/>"
            sXml = sXml & "</input>" &
                "<action arguments=""TAKENtest@1200"" activationType=""background"" content=""Taken!""/>" &
                "<action arguments=""DELAYtest@1200"" activationType=""background"" content=""Delay"" hint-inputId=""delayTime"" />"
        Else
            sActionPrefix = "<action activationType=""background"" arguments=""DELAY" & sActionId
            sXml = sXml & "<action arguments=""TAKENtest@1200"" activationType=""background"" content=""Taken!""/>" &
                sActionPrefix & iMin & """ content=""before""/>" &
                sActionPrefix & iMin & """ content=""after"" />" &
                sActionPrefix & iMin & """ content=""home"" />"
        End If

        sXml = sXml & "<action arguments=""IGNORtest@1200"" activationType=""background"" content=""Ignore"" />"
        sXml = sXml & "</actions>"

        Dim oXml = New Windows.Data.Xml.Dom.XmlDocument
        oXml.LoadXml("<toast scenario=""alarm"">" & sXml & "</toast>")

        Try
            Dim oToast = New Windows.UI.Notifications.ScheduledToastNotification(oXml, oDate,
                TimeSpan.FromMinutes(GetSettingsInt("defaultSnoozeTime", 15)), 5)
            Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().AddToSchedule(oToast)
        Catch ex As Exception
            CrashMessageAdd("@DodajTestowyToast.Add", ex.Message)
        End Try
    End Sub

    Public Shared Sub DodajTestoweToasty()
        DodajTestowyToast(False, Date.Now.AddMinutes(5))
        DodajTestowyToast(True, Date.Now.AddMinutes(5))
    End Sub

    Public Shared Async Function AkcjeSnoozeButtons(oItem As JedenZestaw, sActionId As String, bCanModifyTime As Boolean) As Task(Of String)
        Return Await AkcjeSnoozeWedleKalendarza(oItem, sActionId, bCanModifyTime)
    End Function
    Public Shared Async Function DodajToast(oItem As JedenZestaw, bUseCalendar As Boolean, bCanModifyTime As Boolean) As Task
        Dim sXml As String = "<visual><binding template=""ToastGeneric"">" &
            "<text>" & GetSettingsString("resToastTitle") & ":</text><text>" & oItem.sNazwaZestawu & "</text></binding></visual>" &
            "<actions>"
        ' & " (@ " & Date.Now.ToString("MMdd.HH:mm:ss") 

        Dim sToastId As String = oItem.sId & "@" & oItem.sNextOrgTime & "@"

        If GetSettingsBool("toastListBox") Then
            sXml = sXml & Await AkcjeSnoozeList(oItem, sToastId, bUseCalendar, bCanModifyTime) &
                "<action arguments=""TAKEN" & sToastId & """ activationType=""background"" content=""" & GetSettingsString("resPillTaken") & """/>" &
                "<action arguments=""DELAY" & sToastId & """ activationType=""background"" content=""" & GetSettingsString("resPillDelay") & """ hint-inputId=""delayTime"" />"

            If bUseCalendar AndAlso oItem.oNextTime > Date.Now.AddMinutes(1) Then
                sXml = sXml & "<action arguments=""IGNOR" & sToastId & """ activationType=""background"" content=""" & GetSettingsString("resIgnoreList") & """ />"
            End If

        Else
            sXml = sXml &
                "<action arguments=""TAKEN" & sToastId & """ activationType=""background"" content=""" & GetSettingsString("resPillTaken") & """/>"
            Dim sTmp As String = ""

            If bUseCalendar Then
                sTmp = Await AkcjeSnoozeButtons(oItem, sToastId, bCanModifyTime)
                If sTmp <> "" AndAlso oItem.oNextTime > Date.Now.AddMinutes(1) Then
                    sXml = sXml & sTmp & "<action arguments=""IGNOR" & sToastId & """ activationType=""background"" content=""" & GetSettingsString("resIgnoreButton") & """ />"
                End If
            End If
            If sTmp = "" Then
                sXml = sXml & "<action activationType=""system"" arguments=""snooze"" content=""snooze""/>"
            End If
        End If

        'If bUseCalendar Then
        '    sXml = sXml & "<action arguments=""IGNOR" & sToastId & """ activationType=""background"" content=""Ignore"" />"
        'End If

        sXml = sXml & "</actions>"

        ' "<action arguments='dismiss' activationType='system' content='dismiss'/><action activationType='system' arguments='snooze' content='snooze'/>" " &

        Dim oXml = New Windows.Data.Xml.Dom.XmlDocument
        oXml.LoadXml("<toast scenario=""alarm"">" & sXml & "</toast>")

        Dim oDate As DateTimeOffset = oItem.oNextTime
        If oDate < Date.Now.AddMinutes(5) Then
            CrashMessageAdd("@DodajToast", oItem.sNazwaZestawu & " - zbyt wczesna data")
            oDate = Date.Now.AddMinutes(10)
        End If

        Debug.WriteLine("adding " & oItem.sNazwaZestawu & " for " & oDate.ToString)
        ' 5 kolejnych snooze to maximum

        Try
            Dim oToast = New Windows.UI.Notifications.ScheduledToastNotification(oXml, oDate,
                TimeSpan.FromMinutes(GetSettingsInt("defaultSnoozeTime", 15)), 5)
            Dim sTmp As String = oItem.sNazwaZestawu
            If sTmp.Length > 15 Then sTmp = sTmp.Substring(0, 14)
            oToast.Id = sTmp
            Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().AddToSchedule(oToast)
        Catch ex As Exception
            CrashMessageAdd("@DodajToast.Add", ex.Message)
        End Try
        'Dim oToast = New Windows.UI.Notifications.ScheduledToastNotification(oXml, Date.Now.AddDays(1),
        '        TimeSpan.FromMinutes(GetSettingsInt("defaultSnoozeTime", 15)), 5)

    End Function

    Public Shared Sub UsunToasty()
        Dim oNotifier = Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier()
        Dim oScheduled = oNotifier.GetScheduledToastNotifications()
        For i = 0 To oScheduled.Count - 1
            oNotifier.RemoveFromSchedule(oScheduled.Item(i))
        Next
    End Sub


    Public Shared Async Function ZaplanujToasty() As Task
        UsunToasty()
        For Each oItem As JedenZestaw In App.glZestawy
            If oItem.oNextTime.Year > 9000 Then Continue For
            If Not oItem.bEnabled Then Continue For

            Await App.DodajToast(oItem, True, True)
            ' DodajToast(oItem.sNazwaZestawu, oItem.oNextTime.Value.AddMinutes(oItem.iDelayMins))
        Next
        Await ZestawySave(False) ' tu przeniesione (wcześniej: w AkcjeWedleKalendarza, bo się iDelay tam zmienia
    End Function

    Public Shared Async Function WzialemPigulke(sId As String, sTakeTime As String) As Task
        If sId = "" Then Return

        For Each oItem As JedenZestaw In glZestawy
            If oItem.sId = sId Then
                If oItem.sNextOrgTime = sTakeTime Then
                    Dawkowanie2NextTime(oItem, False)
                    Await DodajToast(oItem, True, True)
                    Await ZestawySave(False)     ' bez zapisywania gdy sie nic nie zmieniło
                End If
                Exit For  ' niezależnie od tego czy sNextOrgTime sie zgadza 
            End If
        Next

    End Function

    Private moAppConn As AppService.AppServiceConnection

    Public Async Function CalledFromBackground(oTask As Windows.ApplicationModel.Background.IBackgroundTaskInstance) As Task(Of Boolean)

        If Not Await ZestawyLoad() Then Return False

        Select Case oTask.Task.Name     '  sTaskname
            Case "WezPigulkeRescheduleToast"
                Dawkowanie2NextTime(True)
                Await ZaplanujToasty()
                Await ZestawySave(False) ' jakby byly zmiany
                Await TriggerNocnyReschedule()
            Case "WezPigulkeCalUpdate"
                If GetSettingsBool("generateToasts") Then
                    ' przeliczenie wszystkiego
                    Dawkowanie2NextTime(True)
                    Await ZaplanujToasty()
                    Await ZestawySave(False) ' jakby byly zmiany
                End If
            Case "WezPigulkeToast"
                Dim oDetails As Windows.UI.Notifications.ToastNotificationActionTriggerDetail = oTask.TriggerDetails
                If oDetails IsNot Nothing Then
                    Dim sGuid As String = oDetails.Argument
                    Dim aArr As String() = sGuid.Substring(5).Split("@")
                    If aArr.GetUpperBound(0) < 1 Then Exit Select    ' wymagamy parametrów (ID oraz taketime)
                    If aArr(0) = "" Then Exit Select   ' ID musi być

                    Select Case sGuid.Substring(0, 5)
                        Case "TAKEN"
                            ' usunąć z listy
                            ' Dim sId As String = sGuid.Substring(5)
                            Await WzialemPigulke(aArr(0), aArr(1))
                        Case "DELAY"
                            For Each oItem As JedenZestaw In glZestawy
                                If oItem.sId = aArr(0) Then
                                    If oItem.sNextOrgTime = aArr(1) Then
                                        If oDetails.UserInput.Count > 0 Then
                                            Dim sIle As String = oDetails.UserInput.Item("delayTime")
                                            If sIle.StartsWith("x") Then
                                                ' snooze 10, 60 min - nie od zera, ale od aktualnego
                                                oItem.iDelayMins = oItem.iDelayMins + sIle.Substring(1)
                                            Else
                                                oItem.iDelayMins = sIle
                                            End If
                                        Else
                                            oItem.iDelayMins = aArr(2)
                                        End If
                                        oItem.oNextTime = oItem.oNextOrgTime.AddMinutes(oItem.iDelayMins)
                                        Await DodajToast(oItem, True, False)
                                        Await ZestawySave(True)     ' bez zapisywania gdy sie nic nie zmieniło
                                    End If

                                    Exit For ' nawet jak aArr(1) <> (czyli zmiana już nastąpiła), to i tak skoncz szukanie
                                End If
                            Next
                        Case "IGNOR"
                            For Each oItem As JedenZestaw In glZestawy
                                If oItem.sId = aArr(0) Then
                                    If oItem.sNextOrgTime = aArr(1) Then

                                        oItem.iDelayMins = 0
                                        oItem.oNextTime = oItem.oNextOrgTime

                                        Await DodajToast(oItem, False, False)
                                        Await ZestawySave(True)     ' bez zapisywania gdy sie nic nie zmieniło
                                    End If

                                    Exit For ' nawet jak aArr(1) <> (czyli zmiana już nastąpiła), to i tak skoncz szukanie
                                End If
                            Next

                    End Select
                End If
            Case Else       ' to moze remote system
                If GetSettingsBool("allowRemoteSystem") Then
                    Dim oDetails As AppService.AppServiceTriggerDetails =
                                    TryCast(oTask.TriggerDetails, AppService.AppServiceTriggerDetails)
                    If oDetails IsNot Nothing Then
                        ' zrob co trzeba
                        AddHandler oTask.Canceled, AddressOf OnTaskCanceled
                        moAppConn = oDetails.AppServiceConnection
                        AddHandler moAppConn.RequestReceived, AddressOf OnRequestReceived
                        ' AddHandler moAppConn.ServiceClosed, AddressOf OnServiceClosed
                    End If

                    Return True     ' nie rób derefere
                End If


        End Select

        Return False
    End Function

    Private Sub OnTaskCanceled(sender As Background.IBackgroundTaskInstance, reason As Background.BackgroundTaskCancellationReason)
        If moTimerDeferal IsNot Nothing Then
            moTimerDeferal.Complete()
            moTimerDeferal = Nothing
        End If
        'If oAppConn IsNot Nothing Then
        '    oAppConn.Dispose()
        '    oAppConn = Nothing
        'End If
    End Sub

    Private Async Sub OnRequestReceived(sender As AppService.AppServiceConnection, args As AppService.AppServiceRequestReceivedEventArgs)
        'Get a deferral so we can use an awaitable API to respond to the message 
        Dim messageDeferral As AppService.AppServiceDeferral = args.GetDeferral()
        Dim oInputMsg As ValueSet = args.Request.Message
        Dim oResultMsg As ValueSet = New ValueSet()
        Dim sResult As String = "ERROR while processing command"
        Try
            Dim sCommand As String = CType(oInputMsg("command"), String)

            Select Case sCommand.ToLower
                Case "ping"
                    sResult = "pong" & vbCrLf &
                        Package.Current.Id.Version.Major & "." &
                            Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build
                    If Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile Then
                        sResult = sResult & vbCrLf & "WIFI"
                    Else
                        sResult = sResult & vbCrLf & "OTHER"
                    End If
                Case "net"
                    If Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWlanConnectionProfile Then
                        sResult = "WIFI"
                    Else
                        sResult = "OTHER"
                    End If
                Case "ver"
                    sResult = Package.Current.Id.Version.Major & "." &
                        Package.Current.Id.Version.Minor & "." & Package.Current.Id.Version.Build
                Case "getzestawy"
                    Dim oSer As Xml.Serialization.XmlSerializer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JedenZestaw)))
                    Dim oStream As Stream = New MemoryStream
                    oSer.Serialize(oStream, glZestawy)
                    oStream.Flush()
                    Try
                        sResult = "FILE"

                        oStream.Seek(0, SeekOrigin.Begin)

                        Dim oRdr As StreamReader = New StreamReader(oStream)
                        Dim sTmp As String = oRdr.ReadToEnd
                        If sTmp.Length > 28000 Then
                            sResult = "ERROR: too much data"
                        Else
                            oResultMsg.Add("content", CType(sTmp, String))
                            'sResult = "OK"
                        End If

                        'oStream.Seek(0, SeekOrigin.Begin)

                        ''Dim oBuff As Windows.Storage.Streams.IBuffer =
                        ''    Await Windows.Storage.FileIO.ReadBufferAsync(oFile)
                        'Dim aByte As Byte()
                        'Dim iLen As Integer = Math.Min(oStream.Length, 30000)
                        'ReDim aByte(iLen)
                        'Await oStream.ReadAsync(aByte, 0, iLen)
                        ''oResultMsg.Add("content", aByte)
                        ''oResultMsg.Add("content", oStream.CType(oBuff.ToArray, Byte()))

                    Catch ex As Exception
                        CrashMessageAdd("@OnRequestReceived:getzestawy", ex.Message)
                    End Try
                Case Else
                    sResult = "ERROR unknown command"
            End Select
        Catch ex As Exception
            CrashMessageAdd("@OnRequestReceived outer Select", ex.Message)
        End Try

        ' odsylamy cokolwiek - zeby "tamta strona" cos zobaczyla
        oResultMsg.Add("result", CType(sResult, String))
        Await args.Request.SendResponseAsync(oResultMsg)

        messageDeferral.Complete()
    End Sub

    Public Shared gsEAN As String = ""
    Public Shared glZnanePudelka As Collection(Of JednoPudelko)

    Const FILENAME_PUDELKA As String = "pudelka.xml"

    Public Shared Async Function ZnanePudelkaLoad() As Task(Of Boolean)
        App.glZnanePudelka = New Collection(Of JednoPudelko)

        Dim oFile As Windows.Storage.StorageFile = Await App.GetRoamingFile(FILENAME_PUDELKA, False)
        If oFile Is Nothing Then Return True    ' czyli jest OK

        Dim oSer As Xml.Serialization.XmlSerializer =
                New Xml.Serialization.XmlSerializer(GetType(Collection(Of JednoPudelko)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Dim bError As Boolean = False
        Try
            glZnanePudelka = TryCast(oSer.Deserialize(oStream), Collection(Of JednoPudelko))
        Catch ex As Exception
            bError = True
        End Try
        oStream.Dispose()
        oStream = Nothing

        If bError Then Return False

        For Each oItem As JednoPudelko In glZnanePudelka
            oItem.bStaly = (oItem.iTypLeku > 0)
            oItem.bIncludeInteraction = oItem.bStaly
        Next

        Return True
    End Function


    Public Shared Async Function ZnanePudelkaSave() As Task

        Dim oFile As Windows.Storage.StorageFile = Await App.GetRoamingFile(FILENAME_PUDELKA, True)
        If oFile Is Nothing Then Exit Function

        Dim oSer As Xml.Serialization.XmlSerializer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JednoPudelko)))
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        oSer.Serialize(oStream, glZnanePudelka)
        oStream.Dispose()   ' == fclose

    End Function

#End Region

#Region "Substancje"
    Public Shared glZnaneSubstancje As Collection(Of JednaSubstancja)

    Const FILENAME_SUBSTANCJE As String = "substancje.xml"

    Public Shared Async Function ZnaneSubstancjeLoad() As Task(Of Boolean)
        App.glZnaneSubstancje = New Collection(Of JednaSubstancja)

        Dim oFile As Windows.Storage.StorageFile = Await App.GetRoamingFile(FILENAME_SUBSTANCJE, False)
        If oFile Is Nothing Then Return True    ' czyli jest OK

        Dim oSer As Xml.Serialization.XmlSerializer =
                New Xml.Serialization.XmlSerializer(GetType(Collection(Of JednaSubstancja)))
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync
        Dim bError As Boolean = False
        Try
            glZnaneSubstancje = TryCast(oSer.Deserialize(oStream), Collection(Of JednaSubstancja))
        Catch ex As Exception
            bError = True
        End Try
        oStream.Dispose()
        oStream = Nothing

        If bError Then Return False

        For Each oItem As JednoPudelko In glZnanePudelka
            oItem.bStaly = (oItem.iTypLeku > 0)
            oItem.bIncludeInteraction = oItem.bStaly
        Next

        Return True
    End Function


    Public Shared Async Function ZnaneSubstancjeSave() As Task

        Dim oFile As Windows.Storage.StorageFile = Await App.GetRoamingFile(FILENAME_SUBSTANCJE, True)
        If oFile Is Nothing Then Exit Function

        Dim oSer As Xml.Serialization.XmlSerializer = New Xml.Serialization.XmlSerializer(GetType(Collection(Of JednaSubstancja)))
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        oSer.Serialize(oStream, glZnaneSubstancje)
        oStream.Dispose()   ' == fclose

    End Function

    Public Shared Sub ZnaneSubstancjeAddChange(oNew As JednaSubstancja)

        For Each oItem As JednaSubstancja In glZnaneSubstancje
            If oItem.sNazwa.ToLower = oNew.sNazwa.ToLower Then
                ' zmiana jeśli ustawione
                If oNew.sId <> "" Then oItem.sId = oNew.sId
                If oNew.sInterAlk <> "?" Then oItem.sInterAlk = oNew.sInterAlk
                If oNew.sInterJedz <> "?" Then oItem.sInterJedz = oNew.sInterJedz
                Return
            End If
        Next
        glZnaneSubstancje.Add(oNew)
    End Sub

#End Region


End Class

