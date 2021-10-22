' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class AddZestaw
    Inherits Page

    Private msIdZestawu As String

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        msIdZestawu = ""
        If e Is Nothing Then Return
        Dim sTmp As String = TryCast(e.Parameter, String)
        If sTmp Is Nothing Then Return
        msIdZestawu = sTmp
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        DodajCBitems
        WypelnijDane(msIdZestawu)
    End Sub

    Private Sub UiAllSame_Toggled(sender As Object, e As RoutedEventArgs) Handles uiAllSame.Toggled
        If uiSchedule Is Nothing Then Return
        uiSchedule.Visibility = If(uiAllSame.IsOn, Visibility.Collapsed, Visibility.Visible)
        'uiCombo9.IsEnabled = uiAllSame.IsOn    - zostaje jako zmieniacz dla wszystkich
        'uiTime9.IsEnabled = uiAllSame.IsOn
    End Sub

    Private Sub UiCombo_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiCombo1.SelectionChanged, uiCombo2.SelectionChanged, uiCombo3.SelectionChanged, uiCombo4.SelectionChanged, uiCombo5.SelectionChanged, uiCombo6.SelectionChanged, uiCombo0.SelectionChanged
        ' w zaleznosci od schedule - zablokuj lub ustaw timepicker
        Dim oCB As ComboBox = TryCast(sender, ComboBox)
        If oCB Is Nothing Then CrashMessageExit("@UiCombo_SelectionChanged", "not Combo?") ' sie nie powinno zdarzyc

        Dim sBaseName As String = oCB.Name
        sBaseName = sBaseName.Replace("Combo", "Time")

        For Each oUiElem As UIElement In uiSchedule.Children
            Dim oTP As TimePicker = TryCast(oUiElem, TimePicker)
            If oTP Is Nothing Then Continue For
            If oTP.Name = sBaseName Then
                oTP.IsEnabled = TryCast(oCB.SelectedValue, ComboBoxItem).ToString.StartsWith("1")
                Exit For
            End If
        Next

    End Sub

    Private Async Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        ' zapisz dane - nowy, albo edytowany
        Dim oNew As JedenZestaw = DaneDoItemu()
        App.ZestawyAdd(oNew)
        Await App.ZestawySave(True)
        Me.Frame.GoBack()
    End Sub

    Private Sub DodajCBitems(oCB As ComboBox)
        Dim oCBitem As ComboBoxItem = New ComboBoxItem
        oCBitem.Content = GetLangString("msgFreq0")     ' "0/d, pauza"
        oCB.Items.Add(oCBitem)
        oCBitem = New ComboBoxItem
        oCBitem.Content = GetLangString("msgFreq1")     '"1/d, raz, o"
        oCB.Items.Add(oCBitem)
        oCBitem = New ComboBoxItem
        oCBitem.Content = GetLangString("msgFreq2")     '"2/d, rano i wieczór, 9/21"
        oCB.Items.Add(oCBitem)
        oCBitem = New ComboBoxItem
        oCBitem.Content = GetLangString("msgFreq3")     '"3/d, co 8 godzin, 8/16/23"
        oCB.Items.Add(oCBitem)
    End Sub

    Private Sub DodajCBitems()

        ' do combo ogolnego
        DodajCBitems(uiCombo9)
        ' do kazdego combo dnia tygodnia dodaj zawartosc
        For Each oUiElem As UIElement In uiSchedule.Children
            Dim oCB As ComboBox = TryCast(oUiElem, ComboBox)
            If oCB Is Nothing Then Continue For
            DodajCBitems(oCB)
        Next
    End Sub

    Private Sub WypelnijDane(msIdZestawu As String)
        If msIdZestawu = "" Then Return

        ' dane z zestawu na ekran
        For Each oItem As JedenZestaw In App.glZestawy
            If oItem.sId = msIdZestawu Then
                uiNazwaZestawu.Text = oItem.sNazwaZestawu
                uiMelodyjka.Text = oItem.sMelodyjka
                uiEnabled.IsChecked = oItem.bEnabled

                Dim aArr As String() = oItem.sTakeTimes.Split("|")
                Dim bAllSame As Boolean = True
                If aArr.GetUpperBound(0) > 6 Then
                    For i As Integer = 1 To 6
                        If aArr.GetUpperBound(0) < i Then Exit For
                        If aArr(i) <> aArr(0) Then
                            bAllSame = False
                            Exit For
                        End If
                    Next

                End If

                If bAllSame Then
                    uiAllSame.IsOn = True
                    uiSchedule.Visibility = Visibility.Collapsed
                    uiCombo9.IsEnabled = True
                    uiTime9.IsEnabled = True
                    SetRow(uiCombo9, uiTime9, aArr(0))
                Else
                    uiAllSame.IsOn = False
                    uiSchedule.Visibility = Visibility.Visible
                    uiCombo9.IsEnabled = False
                    uiTime9.IsEnabled = False

                    For i As Integer = 0 To 6
                        SetJedenRow(i, aArr(i))
                    Next
                End If

            End If
        Next
    End Sub

    Private Sub SetRow(oCB As ComboBox, oTP As TimePicker, sSchedule As String)
        For Each oItem As ComboBoxItem In oCB.Items
            If oItem.Content.ToString.Substring(0, 1) = sSchedule.Substring(0, 1) Then
                oCB.SelectedItem = oItem
                Exit For
                'oItem.IsSelected = True
            Else
                'oItem.IsSelected = False
            End If
        Next

        oTP.Time = New TimeSpan(sSchedule.Substring(2, 2), sSchedule.Substring(4, 2), 0)
    End Sub

    Private Sub SetJedenRow(iInd As Integer, sSchedule As String)
        Dim oCB As ComboBox = Nothing
        Dim oTP As TimePicker = Nothing
        For Each oItem As UIElement In uiSchedule.Children
            Dim oTmpCB As ComboBox = TryCast(oItem, ComboBox)
            If oTmpCB.Name.EndsWith(iInd.ToString) Then oCB = oTmpCB
            Dim oTmpTP As TimePicker = TryCast(oItem, TimePicker)
            If oTmpTP.Name.EndsWith(iInd.ToString) Then oTP = oTmpTP
        Next
        If oCB Is Nothing Then CrashMessageExit("@SetJedenRow", "no oCB?")
        If oTP Is Nothing Then CrashMessageExit("@SetJedenRow", "no oTP?")

        SetRow(oCB, oTP, sSchedule)
    End Sub

    Private Function DaneDoItemu() As JedenZestaw
        Dim oNew As JedenZestaw = New JedenZestaw
        If msIdZestawu <> "" Then
            oNew.sId = msIdZestawu
        Else
            oNew.sId = Date.Now.ToString("yyMMdd.HHmmss")
        End If

        oNew.sNazwaZestawu = uiNazwaZestawu.Text
        oNew.sMelodyjka = uiMelodyjka.Text
        oNew.bEnabled = uiEnabled.IsChecked
        oNew.iDelayMins = 0

        If uiAllSame.IsOn Then
            oNew.sTakeTimes = TryCast(uiCombo9.SelectedValue, ComboBoxItem).Content.ToString.Substring(0, 1) & "/" &
                        uiTime9.Time.ToString("hhmm")
        Else
            oNew.sTakeTimes = TryCast(uiCombo0.SelectedValue, ComboBoxItem).Content.ToString.Substring(0, 1) & "/" &
                        uiTime0.Time.ToString("hhmm") & "|" &
                        TryCast(uiCombo1.SelectedValue, ComboBoxItem).Content.ToString.Substring(0, 1) & "/" &
                        uiTime1.Time.ToString("hhmm") & "|" &
                        TryCast(uiCombo2.SelectedValue, ComboBoxItem).Content.ToString.Substring(0, 1) & "/" &
                        uiTime2.Time.ToString("hhmm") & "|" &
                        TryCast(uiCombo3.SelectedValue, ComboBoxItem).Content.ToString.Substring(0, 1) & "/" &
                        uiTime3.Time.ToString("hhmm") & "|" &
                        TryCast(uiCombo4.SelectedValue, ComboBoxItem).Content.ToString.Substring(0, 1) & "/" &
                        uiTime4.Time.ToString("hhmm") & "|" &
                        TryCast(uiCombo5.SelectedValue, ComboBoxItem).Content.ToString.Substring(0, 1) & "/" &
                        uiTime5.Time.ToString("hhmm") & "|" &
                        TryCast(uiCombo6.SelectedValue, ComboBoxItem).Content.ToString.Substring(0, 1) & "/" &
                        uiTime6.Time.ToString("hhmm") & "|"
        End If

        Return oNew
    End Function

    Private Sub UiCombo9_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiCombo9.SelectionChanged
        If uiAllSame.IsOn Then

            Dim sAllSel As String = TryCast(uiCombo9.SelectedItem, ComboBoxItem).Content.ToString.Substring(0, 1)

            For Each oUiElem As UIElement In uiSchedule.Children
                Dim oCB As ComboBox = TryCast(oUiElem, ComboBox)
                If oCB Is Nothing Then Continue For
                For Each oItem As ComboBoxItem In oCB.Items
                    oItem.IsSelected = (oItem.Content.ToString.Substring(0, 1) = sAllSel)
                Next
            Next

        End If
    End Sub

    Private Sub UiTime9_TimeChanged(sender As Object, e As TimePickerValueChangedEventArgs) Handles uiTime9.TimeChanged
        If uiAllSame.IsOn Then
            uiTime0.Time = uiTime9.Time
            uiTime1.Time = uiTime9.Time
            uiTime2.Time = uiTime9.Time
            uiTime3.Time = uiTime9.Time
            uiTime4.Time = uiTime9.Time
            uiTime5.Time = uiTime9.Time
            uiTime6.Time = uiTime9.Time
        End If

    End Sub
End Class
