
' lista leków które już znamy (roaming) - wedle typu, potem nazwy (czynnej?)
' przy każdym do wyboru typ leku (stały, etc.)
' context: zmiana typu, pokaż ulotkę, pokaż charakterystykę, 

' z dolnego menu: ściągnij ulotkę i charakterystykę (do localcache albo local, nie roaming!)
' szukaj interakcji (wedle nazwy czynnej)

' refresh danych? jakby doszly nowe EANy na przyklad?

Public NotInheritable Class MojeLeki
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

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiList.ItemsSource = From c In App.glZnanePudelka Order By c.iTypLeku Descending, c.sNazwa
    End Sub

    Private Sub uiShowDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem
        oMFI = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPudelko = TryCast(oMFI.DataContext, JednoPudelko)
        If oItem Is Nothing Then Return
        Me.Frame.Navigate(GetType(DaneLeku), oItem.sBarcode)
    End Sub

    Private Sub uiShowUlotka_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem
        oMFI = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPudelko = TryCast(oMFI.DataContext, JednoPudelko)
        If oItem Is Nothing Then Return

    End Sub

    Private Sub uiShowCharakt_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem
        oMFI = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPudelko = TryCast(oMFI.DataContext, JednoPudelko)
        If oItem Is Nothing Then Return

    End Sub

    Private Async Function ZmianaTypu(sender As Object, bStaly As Boolean) As Task
        Dim oMFI As MenuFlyoutItem
        oMFI = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPudelko = TryCast(oMFI.DataContext, JednoPudelko)
        If oItem Is Nothing Then Return

        If oItem.bStaly = bStaly Then Return
        oItem.bStaly = bStaly
        oItem.iTypLeku = If(bStaly, 1, 0)
        Await App.ZnanePudelkaSave()
        uiList.ItemsSource = From c In App.glZnanePudelka Order By c.iTypLeku Descending, c.sNazwa
    End Function

    Private Sub uiSetStaly_Click(sender As Object, e As RoutedEventArgs)
        ZmianaTypu(sender, True)
    End Sub

    Private Sub uiSetTemp_Click(sender As Object, e As RoutedEventArgs)
        ZmianaTypu(sender, False)
    End Sub

    Private Async Function Interakcje_SciagnijDaneSubstancji(oLek As JednoPudelko) As Task(Of String)
        ' zwraca sId substancji

        Dim oSubst As JednaSubstancja

        For Each oSubst In App.glZnaneSubstancje
            If oSubst.sNazwa.ToLower = oLek.sNazwaCzynna.ToLower Then Return oSubst.sId
        Next

        oSubst = New JednaSubstancja
        oSubst.sNazwa = oLek.sNazwaCzynna

        If moHttp Is Nothing Then
            moHttp = New Windows.Web.Http.HttpClient
        End If

        Progresuj(True)

        Dim sBaseUri As String = "https://ktomalek.pl/l/interakcje/"
        Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing
        Dim sResp As String = ""
        oResp = Await moHttp.GetAsync(New Uri(sBaseUri & "pomiedzy-lekami"))
        sResp = Await oResp.Content.ReadAsStringAsync
        Await Task.Delay(100)   ' poczekajmy

        Dim sUri As String
        sUri = sBaseUri & "szukajBrandowISubstancjiAjax?searchInput=" & oLek.sNazwaCzynna

        oResp = Await moHttp.GetAsync(New Uri(sUri))
        sResp = Await oResp.Content.ReadAsStringAsync

        Dim iInd As Integer
        iInd = sResp.IndexOf("Znalezione substancje czynne")
        If iInd < 1 Then
            Progresuj(False)
            Return ""
        End If

        sResp = sResp.Substring(iInd)
        ' brand.dodajDoSesji(1461, &apos;RAMIPRILUM&apos;, &apos;SUBSTANCJA&apos;
        ' zamienic na
        ' https://ktomalek.pl/l/interakcje/dodajDoSesjiAjax?id=1461&nazwa=RAMIPRILUM&typ=SUBSTANCJA
        iInd = sResp.IndexOf("dodajDoSesji")
        If iInd > 0 Then
            sResp = sResp.Substring(iInd + 13)
            iInd = sResp.IndexOf(",")
            If iInd > 0 Then oSubst.sId = sResp.Substring(0, iInd)
        End If
        If iInd < 1 Then
            Progresuj(False)
            Return ""
        End If

        'sResp = sResp.Substring(iInd)
        'iInd = sResp.IndexOf(";")
        'If iInd < 1 Then Return
        'sResp = sResp.Substring(iInd + 1)
        'iInd = sResp.IndexOf("&")
        'If iInd < 1 Then Return
        'sResp = sResp.Substring(0, iInd)
        'oItem.sIdSubstancji = oItem.sIdSubstancji & "&nazwa=" & sResp

        App.ZnaneSubstancjeAddChange(oSubst)
        Await App.ZnaneSubstancjeSave     ' zapisz, jakby potem był błąd - ten krok będzie można pominąć

        Progresuj(False)
        Return oSubst.sId

    End Function


    Private Function InterakcjeLekow_SprawdzZaznaczenia() As Integer
        'Dim iLekiCnt As Integer = 0
        'For Each oLek As JednoPudelko In From c In App.glZnanePudelka Where c.bIncludeInteraction
        '    iLekiCnt += 1
        'Next
        'Return iLekiCnt
        Return (From c In App.glZnanePudelka Where c.bIncludeInteraction).Count
    End Function

    Private Async Function InterakcjeLekow_ZnamSubstancje() As Task(Of Boolean)
        For Each oLek As JednoPudelko In From c In App.glZnanePudelka Where c.bIncludeInteraction
            Dim bFound As Boolean = False
            For Each oSubst As JednaSubstancja In App.glZnaneSubstancje
                If oSubst.sNazwa.ToLower = oLek.sNazwaCzynna.ToLower Then
                    bFound = True
                    Exit For
                End If
            Next
            If Not bFound Then
                Dim sSubstId As String = Await Interakcje_SciagnijDaneSubstancji(oLek)
                If sSubstId = "" Then Return False
            End If
        Next
        Return True
    End Function

    Private Async Sub uiInterakcje_Click(sender As Object, e As RoutedEventArgs)
        ' dla kazdego ktory jest bIncludeInteraction
        ' https://ktomalek.pl/l/interakcje/lekow-z-zywnoscia
        ' GET https://ktomalek.pl/l/interakcje/dodajDoSesjiAjax?id=1461&nazwa=RAMIPRILUM&typ=SUBSTANCJA
        ' xhtmlrequest
        ' zwraca: cos jakby url, ktory potem jest aktualny
        ' Nie posiadamy informacji wskazujących, aby wybrana substancja wchodziła w interakcje z żywnością

        ' https://ktomalek.pl/l/interakcje/pomiedzy-lekami

        If InterakcjeLekow_SprawdzZaznaczenia() < 2 Then
            DialogBox("Interakcje między lekami wymagają zaznaczenia przynajmniej dwu leków")
            Return
        End If

        Progresuj(True)
        If Not Await InterakcjeLekow_ZnamSubstancje() Then
            Await DialogBox("Nie znam substancji do niektórych leków!")
            ' *TODO* tylko ignorowanie, bo:
            ' lek bez substancji można zignorować (albo dodać jako lek, nie jako substancje)
            Progresuj(False)
            Return
        End If

        'If moHttp Is Nothing Then
        '    moHttp = New Windows.Web.Http.HttpClient
        'End If

        'Dim sBaseUri As String = "https://ktomalek.pl/l/interakcje/"
        'Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing
        'Dim sResp As String = ""
        'oResp = Await moHttp.GetAsync(New Uri(sBaseUri & "pomiedzy-lekami"))
        'sResp = Await oResp.Content.ReadAsStringAsync
        'Await Task.Delay(100)   ' poczekajmy

        'For Each oLek In From c In App.glZnanePudelka Where c.bIncludeInteraction
        '    For Each oSubst As JednaSubstancja In App.glZnaneSubstancje
        '        If oSubst.sNazwa.ToLower = oLek.sNazwaCzynna.ToLower Then
        '            ' dodaj do interakcji
        '            Dim sUri As String = sBaseUri & "pomiedzy-lekami/a/su-" & oSubst.sId
        '            oResp = Await moHttp.GetAsync(New Uri(sUri))
        '            sResp = Await oResp.Content.ReadAsStringAsync
        '            Await Task.Delay(100)   ' poczekajmy
        '            Exit For
        '        End If
        '    Next
        'Next

        '' sResp teoretycznie ma dane o interakcjach
        'Dim iInd As Integer = 0

        Progresuj(False)
        Await DialogBox("Jeszcze nie umiem")

    End Sub

    Private Async Function PokazInterakcje(sender As Object, bJedzenie As Boolean) As Task
        ' lek z alkoholem/zywnoscia , nie pomiedzy lekami

        Dim oMFI As MenuFlyoutItem
        oMFI = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPudelko = TryCast(oMFI.DataContext, JednoPudelko)
        If oItem Is Nothing Then Return

        Dim oSubst As JednaSubstancja = Nothing

        Dim sSubstId As String = Await Interakcje_SciagnijDaneSubstancji(oItem)
        If sSubstId = "" Then
            Await DialogBox("Nie znalazłem substancji czynnej?")
            Return
        End If

        For Each oSubst In App.glZnaneSubstancje
            If oSubst.sId = sSubstId Then
                Dim sMsg As String = ""
                sMsg = If(bJedzenie, oSubst.sInterJedz, oSubst.sInterAlk)
                If sMsg.Length > 2 Then
                    Await DialogBox(sMsg)
                    Return
                End If
                Exit For
            End If
        Next

        'If Not bFound Then oSubst = New JednaSubstancja
        'oSubst.sNazwa = oItem.sNazwaCzynna

        Dim sLink As String = If(bJedzenie, "lekow-z-zywnoscia", "lekow-z-alkoholem")

        If moHttp Is Nothing Then
            moHttp = New Windows.Web.Http.HttpClient
        End If

        Progresuj(True)

        Dim sBaseUri As String = "https://ktomalek.pl/l/interakcje/"
        Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing
        Dim sResp As String = ""
        oResp = Await moHttp.GetAsync(New Uri(sBaseUri & sLink))
        sResp = Await oResp.Content.ReadAsStringAsync
        Await Task.Delay(100)   ' poczekajmy

        Dim sUri As String = sBaseUri & sLink & "/a/su-" & sSubstId

        'sUri = sBaseUri & "dodajDoSesjiAjax?id=" & oItem.sIdSubstancji & "&typ=SUBSTANCJA"
        oResp = Await moHttp.GetAsync(New Uri(sUri))
        sResp = Await oResp.Content.ReadAsStringAsync

        ' zdaje sie ze jak tak jest, to znaczy ze nie ma interakcji

        If sResp.Contains("message-valid") AndAlso sResp.Contains("Nie posiadamy informacji wskazujących") Then
            If bJedzenie Then
                oSubst.sInterJedz = "Brak interakcji"
            Else
                oSubst.sInterAlk = "Brak interakcji"
            End If
            App.ZnaneSubstancjeAddChange(oSubst)
            Dim oTask As Task = App.ZnaneSubstancjeSave
            Await DialogBox("Brak interakcji")
            Await oTask ' wykorzystujemy czas dialogboxa
            Progresuj(False)
            Return
        End If

        ' a teraz moze jest interakcja
        ' box panel
        Dim iInd As Integer
        If sResp.Contains("<article") Then
            iInd = sResp.IndexOf("<article")
            sResp = sResp.Substring(iInd)
            iInd = sResp.IndexOf("</article")
            sResp = sResp.Substring(0, iInd)

            iInd = sResp.IndexOf("<h3")
            sResp = sResp.Substring(iInd)
            iInd = sResp.IndexOf("</h3")
            Dim sInterakcja As String = RemoveHtmlTags(sResp.Substring(0, iInd)) ' istotna, etc.

            iInd = sResp.IndexOf("<p", iInd)
            sResp = sResp.Substring(iInd)

            sInterakcja = sInterakcja & vbCrLf & RemoveHtmlTags(sResp)

            If bJedzenie Then
                oSubst.sInterJedz = sInterakcja
            Else
                oSubst.sInterAlk = sInterakcja
            End If
            App.ZnaneSubstancjeAddChange(oSubst)
            Dim oTask As Task = App.ZnaneSubstancjeSave
            Await DialogBox(sInterakcja)
            Await oTask ' wykorzystujemy czas dialogboxa
        Else
            Await DialogBox("Nie rozumiem odpowiedzi serwera")
        End If

        Progresuj(False)


        ' https://ktomalek.pl/l/interakcje/szukajBrandowISubstancjiAjax?searchInput=rami

    End Function

    Private Sub uiInterJedz_Click(sender As Object, e As RoutedEventArgs)
        ' ewentualnie jakiś cache do tego, typu substancjaczynna i jej interakcje
        ' albo lek i jego interakcje, jak nie ma substancji czynnej (bądź: wiele, jak broncho)
        ' https://ktomalek.pl/l/interakcje/lekow-z-zywnoscia
        PokazInterakcje(sender, True)
    End Sub

    Private Sub uiInterAlk_Click(sender As Object, e As RoutedEventArgs)
        ' https://ktomalek.pl/l/interakcje/lekow-z-alkoholem
        PokazInterakcje(sender, False)
    End Sub
End Class
