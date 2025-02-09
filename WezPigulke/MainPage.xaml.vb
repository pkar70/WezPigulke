' v1.1906.19
' 2019.06.11 App:RemoSys: poprawka odsyłania danych
' 2019.06.12 Setting: ignore if Location contains text... (local setting)
' 2019.06.17 App:Dawkowanie2NextTime, poprawka limitu iFor (>= na >)

'Imports vblib
Imports pkar.UI.Extensions
Imports pkar

Public NotInheritable Class MainPage
    Inherits Page

    Dim mbInLoading As Boolean = True


    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        'Await DodajWezPigulke()    - plik wrzuc, jak go nie ma w Sounds

        ' w App jest wczytanie glZestawy (lub import)
        Dim berr As Boolean = False
        Try
            If vblib.globalsy.glZnanePudelka Is Nothing Then
                vblib.globalsy.glZnanePudelka = New BaseList(Of vblib.JednoPudelko)(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "pudelka.json")
                vblib.globalsy.glZnanePudelka.Load()

                If vblib.globalsy.glZnanePudelka.Count < 1 Then
                    'Await App.ZnanePudelkaImportXML  ' false bedzie pewnie zwykle - dla nie-Polaków
                End If
            End If

        Catch ex As Exception
            berr = True
        End Try
        If berr Then Await Me.MsgBoxAsync("ERROR PUDELKA")


        Try

            If vblib.globalsy.glZnaneSubstancje Is Nothing Then
                vblib.globalsy.glZnaneSubstancje = New BaseList(Of vblib.JednaSubstancja)(Windows.Storage.ApplicationData.Current.RoamingFolder.Path, "substancje.json")
                vblib.globalsy.glZnaneSubstancje.Load()

                If vblib.globalsy.glZnaneSubstancje.Count < 1 Then
                    ' Await App.ZnaneSubstancjeImportXML  ' false bedzie pewnie zwykle - dla nie-Polaków
                End If

            End If
        Catch ex As Exception
            berr = True
        End Try
        If berr Then Await Me.MsgBoxAsync("ERROR SUBSTANCJE")

        Try

            If vblib.GetSettingsBool("generateToasts") Then
                ' reinit nexttime - moze był okres bez aplikacji, i samo się nie zmieniało
                vblib.globalsy.Dawkowanie2NextTime(True)
                Await App.ZaplanujToasty()
            End If
        Catch ex As Exception
            berr = True
        End Try
        If berr Then Await Me.MsgBoxAsync("ERROR TOASTY")

        mbInLoading = True  ' zeby nie zapisywal tego co wlasnie odczytal

        If vblib.globalsy.glZestawy Is Nothing Then
            Await Me.MsgBoxAsync("Nie wczytalem jeszcze zestawów")
            Await App.WczytajZestawyLubImportuj
            If vblib.globalsy.glZestawy Is Nothing Then
                Await Me.MsgBoxAsync("Dalej nie mam zestawów")
            End If
        End If

        If vblib.globalsy.glZestawy IsNot Nothing Then
            Await Me.MsgBoxAsync("mam zestawy")
            uiList.ItemsSource = From c In vblib.globalsy.glZestawy Order By c.oNextTime
        End If

        Try
            Await App.RegisterTriggers()
        Catch ex As Exception
            berr = True
        End Try
        If berr Then Await Me.MsgBoxAsync("ERROR TRIGGERS")

        'Await CrashMessageShow()

        'SetSettingsString("resBeforeList", GetLangString("resBeforeList"))
        'SetSettingsString("resAfterList", GetLangString("resAfterList"))
        'SetSettingsString("resIgnoreList", GetLangString("resIgnoreList"))
        'SetSettingsString("resHomeList", GetLangString("resHomeList"))
        'SetSettingsString("resBeforeButton", GetLangString("resBeforeButton"))
        'SetSettingsString("resAfterButton", GetLangString("resAfterButton"))
        'SetSettingsString("resIgnoreButton", GetLangString("resIgnoreButton"))
        'SetSettingsString("resHomeButton", GetLangString("resHomeButton"))
        'SetSettingsString("resToastTitle", GetLangString("resToastTitle"))
        'SetSettingsString("resPillTaken", GetLangString("resPillTaken"))
        'SetSettingsString("resPillDelay", GetLangString("resPillDelay"))

        Await Task.Delay(1000)   ' musi poczekac - inaczej jest wywolywany Event enable/disable
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

    Private Function Sender2Zestaw(sender As Object) As vblib.JedenZestaw
        Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        If oFE?.DataContext Is Nothing Then Return Nothing

        Return TryCast(oFE.DataContext, vblib.JedenZestaw)
    End Function


    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As vblib.JedenZestaw = Sender2Zestaw(sender)
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
            If vblib.GetLangString("_lang") = "PL" Then bPL = True
        Catch
        End Try

        If Not bPL AndAlso Not vblib.GetSettingsBool("warningPLshown") Then
            Me.MsgBox("This functionality is designed to work in Poland only")
            vblib.SetSettingsBool("warningPLshown", True)
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
        Dim oItem As vblib.JedenZestaw = Sender2Zestaw(sender)
        If oItem Is Nothing Then Return
        oItem.bEnabled = Not oItem.bEnabled
        mbInLoading = True  ' zeby nie zapisywal tego co wlasnie odczytal
        uiList.ItemsSource = From c In vblib.globalsy.glZestawy Order By c.oNextTime
        vblib.globalsy.glZestawy.Save() '.ZestawySave(True)
        Await Task.Delay(500)
        mbInLoading = False
    End Sub

    Private Sub uiDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As vblib.JedenZestaw = Sender2Zestaw(sender)
        If oItem Is Nothing Then Return

        'Dim sTxt As String = "Details" & vbCrLf
        'sTxt = sTxt & "sId=" & oItem.sId & vbCrLf
        'sTxt = sTxt & "sNazwaZestawu=" & oItem.sNazwaZestawu & vbCrLf
        'sTxt = sTxt & "sTakeTimes=" & oItem.sTakeTimes & vbCrLf
        'sTxt = sTxt & "sNextOrgTime=" & oItem.sNextOrgTime & vbCrLf
        'sTxt = sTxt & "iDelayMins=" & oItem.iDelayMins & vbCrLf
        'sTxt = sTxt & "bEnabled=" & oItem.bEnabled & vbCrLf
        ''sTxt = sTxt & "bEnabled=" & oItem.bEnabled & vbCrLf
        'sTxt = sTxt & "oNextOrgTime=" & oItem.oNextOrgTime.ToString("yyyy.MM.dd HH:mm:ss") & vbCrLf
        'sTxt = sTxt & "oNextTime=" & oItem.oNextTime.ToString("yyyy.MM.dd HH:mm:ss") & vbCrLf
        'sTxt = sTxt & "sDisplayTime=" & oItem.sDisplayTime & vbCrLf
        'sTxt = sTxt & "sDisplayOrgTime=" & oItem.sDisplayOrgTime & vbCrLf

        Me.MsgBox("Details" & vbCrLf & oItem.DumpAsText)
    End Sub
End Class
