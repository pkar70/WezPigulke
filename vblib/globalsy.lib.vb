Imports System.Text
Imports pkar

Public Module globalsy
    Public glZnanePudelka As BaseList(Of JednoPudelko)

    Public glZestawy As BaseList(Of JedenZestaw)

    Public glZnaneSubstancje As BaseList(Of JednaSubstancja)

    Public glWycofaniaGIF As WycofaniaGIF

    Friend moHttp As New Net.Http.HttpClient

    Public Sub ZnaneSubstancjeAddChange(oNew As vblib.JednaSubstancja)

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

    Public Sub ZestawyAdd(oNew As JedenZestaw)
        Dim bToEdit As Boolean = False

        For Each oItem As JedenZestaw In glZestawy
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
            glZestawy.Add(oNew)
        End If

        glZestawy.Save()

    End Sub

    Public Sub ZestawyUpdateTimers()

        ' wylicz oNextTime, sNextTime, iMinsToTake 
        For Each oItem As JedenZestaw In glZestawy
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

            'Dim bMoje As Boolean = IsThisMoje()
            'oItem.bIsThisMoje = bMoje
        Next
    End Sub


    ''' <summary>
    ''' to zmienia glZestawy!
    ''' </summary>
    ''' <param name="oItem"></param>
    ''' <param name="bReset"></param>
    Public Sub Dawkowanie2NextTime(oItem As vblib.JedenZestaw, bReset As Boolean)
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

        'bZestawyDirty = True
    End Sub

    Public Sub Dawkowanie2NextTime(bReset As Boolean)
        For Each oItem As vblib.JedenZestaw In vblib.globalsy.glZestawy

            If bReset OrElse oItem.oNextTime.Year > 9000 Then
                ' wylicz next time
                Dawkowanie2NextTime(oItem, bReset)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Sprawdź wycofania wg istniejących list leków oraz wycofań (nic nie wczytuje). 
    ''' </summary>
    ''' <returns>NULL gdy którejś listy nie ma, Empty gdy brak wycofań, lub string z wycofaniami</returns>
    Public Function SprawdzWycofania(bIgnoreArchLeki As Boolean) As String

        If glZnanePudelka Is Nothing Then Return Nothing
        If glWycofaniaGIF Is Nothing Then Return Nothing

        Dim ret As String = ""

        For Each lek As JednoPudelko In glZnanePudelka

            If bIgnoreArchLeki AndAlso lek.iTypLeku = 0 Then Continue For

            Select Case glWycofaniaGIF.CheckEAN(lek.sBarcode)
                Case GIFstatus.Wycofany
                    If lek.bWasToasted <> GIFstatus.Wycofany Then
                        ret &= vbCrLf & "Wycofany lek: " & lek.sNazwa
                        lek.bWasToasted = GIFstatus.Wycofany
                    End If
                Case GIFstatus.Wstrzymany
                    If lek.bWasToasted <> GIFstatus.Wstrzymany Then
                        ret &= vbCrLf & "Wstrzymany lek: " & lek.sNazwa
                        lek.bWasToasted = GIFstatus.Wstrzymany
                    End If
                Case GIFstatus.Wznowiony
                    If lek.bWasToasted = GIFstatus.Wycofany Then
                        ret &= vbCrLf & "Wznowiony lek: " & lek.sNazwa
                        lek.bWasToasted = GIFstatus.Wznowiony
                    End If
            End Select
        Next

        Return ret

    End Function


End Module