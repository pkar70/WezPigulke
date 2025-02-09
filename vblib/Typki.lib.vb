
' to, o czym przypominamy

Imports pkar

Public Class JedenZestaw
    Inherits BaseStruct

    Public Property sId As String
    Public Property sNazwaZestawu As String
    Public Property sTakeTimes As String
    Public Property sMelodyjka As String
    Public Property sNextOrgTime As String     ' taki jaki wynika z scheduler (bez 'snooze')
    Public Property iDelayMins As Integer   ' do tego dodawany jest 'snooze'
    Public Property bEnabled As Boolean

    <Newtonsoft.Json.JsonIgnore>
    Public Property oNextOrgTime As DateTimeOffset = New DateTime(9001, 12, 31)   ' poza sortowaniem
    <Newtonsoft.Json.JsonIgnore>
    Public Property oNextTime As DateTimeOffset = New DateTime(9001, 12, 31)
    <Newtonsoft.Json.JsonIgnore>
    Public Property sDisplayTime As String = ""
    <Newtonsoft.Json.JsonIgnore>
    Public Property sDisplayOrgTime As String = ""
    <Newtonsoft.Json.JsonIgnore>
    Public Property IsThisMoje As Boolean
End Class

' zakup leku - ten sam lek moze miec rozne pudelka (roznych producentow)

Public Class JednoPudelko
    Public Property sBarcode As String
    Public Property sNazwa As String
    Public Property sNazwaPowszechna As String
    Public Property sMoc As String
    Public Property sPostac As String
    Public Property sPozwolenie As String
    Public Property sWaznosc As String
    Public Property sPodmiot As String
    Public Property sProcedura As String
    Public Property sDetailsLink As String
    Public Property sCreated As String
    Public Property sNazwaCzynna As String
    Public Property sKodATC As String
    'Public Property bWycofane As Boolean
    Public Property sOpakowania As String
    'Public Property sDawneOpakowania As String
    ''' <summary>
    ''' 0-arch/chwilowy, 1-apteczka/dłuższy, 2-stały (do interakcji)
    ''' </summary>
    Public Property iTypLeku As Integer = 0   ' 

    Public Property IDrpl As String

    <Newtonsoft.Json.JsonIgnore>
    Public Property bIncludeInteraction As Boolean = False
    <Newtonsoft.Json.JsonIgnore>
    Public Property bWasToasted As GIFstatus = GIFstatus.NoInfo


    Private Function ZnajdzSubstancjeCached() As JednaSubstancja
        For Each oSubst As JednaSubstancja In vblib.globalsy.glZnaneSubstancje
            If oSubst.sNazwa.ToLower = sNazwaCzynna.ToLower Then Return oSubst
        Next

        Return Nothing
    End Function

    ''' <summary>
    ''' znajduje substancję w cache, bądź tworzy nową (ściąga ID dla nazwy leku). NIE zapisuje pliku subsntancji.
    ''' </summary>
    Public Async Function ZnajdzSubstancje() As Task(Of JednaSubstancja)
        Dim oSubst As JednaSubstancja = ZnajdzSubstancjeCached()

        If oSubst IsNot Nothing Then Return oSubst

        Dim sBaseUri As String = "https://ktomalek.pl/l/interakcje/"
        Dim oResp As Net.Http.HttpResponseMessage = Nothing
        Dim sResp As String = ""
        oResp = Await moHttp.GetAsync(New Uri(sBaseUri & "pomiedzy-lekami"))
        sResp = Await oResp.Content.ReadAsStringAsync
        Await Task.Delay(100)   ' poczekajmy

        Dim sUri As String
        sUri = sBaseUri & "szukajBrandowISubstancjiAjax?searchInput=" & sNazwa.Replace(" ", "%20")

        oResp = Await moHttp.GetAsync(New Uri(sUri))
        sResp = Await oResp.Content.ReadAsStringAsync


        If sResp.NotContains("Znalezione leki") Then
            Return Nothing
        End If

        oSubst = New JednaSubstancja With {.sNazwa = sNazwaCzynna}

        Try

            '<h2>BISORATIO</h2>
            '<input type = "hidden" id="pobranoBrandy_66346" value="N" autocomplete="off"/>

            ' może też być wg substancji czynnych - ale bisoprololum, bez fumaras?

            Dim iInd As Integer = sResp.IndexOf("<h2>" & sNazwa.ToUpper & "</h2>")
            If iInd < 1 Then Return Nothing
            sResp = sResp.Substring(iInd + 10)

            ' na wszelki wypadek ucinamy na początku kolejnego wpisu (jesli istnieje)
            iInd = sResp.IndexOf("<h2>")
            If iInd > 1 Then sResp = sResp.Substring(0, iInd)

            iInd = sResp.IndexOf("pobranoBrandy_")
            If iInd < 1 Then Return Nothing

            sResp = sResp.Substring(iInd + "pobranoBrandy_".Length, 15)
            iInd = sResp.IndexOf("""")
            sResp = sResp.Substring(0, iInd)

            oSubst.sId = sResp

        Catch ex As Exception
            Return Nothing
        End Try

        vblib.globalsy.ZnaneSubstancjeAddChange(oSubst)

        Return oSubst

    End Function

End Class

Public Class JedenLek
    Public Property sId As String
    Public Property sNazwaLeku As String
    Public Property sSubstCzynna As String
    Public Property sNazwaZestawu As String
    Public Property bImportant As Boolean
    Public Property sPrzyjmowanieOd As String
    Public Property sPrzyjmowanieDo As String
End Class

Public Class JednaSubstancja
    Public Property sId As String
    Public Property sNazwa As String
    Public Property sInterJedz As String = "?"
    Public Property sInterAlk As String = "?"

    ''' <summary>
    ''' zwraca tekst interakcji - jeśli nie ma, to go ściąga. NIE zapisuje pliku substancji!
    ''' </summary>
    ''' <param name="bJedzenie">TRUE gdy chodzi o jedzenie, FALSE gdy chodzi o alkohol</param>
    Public Async Function GetWebInterakcje(bJedzenie As Boolean) As Task(Of String)

        Dim ret As String = If(bJedzenie, sInterJedz, sInterAlk)
        If ret.Length > 2 Then Return ret

        ' nie wiemy nic o interakcjach i trzeba uzupełnić

        Dim sLink As String = If(bJedzenie, "lekow-z-zywnoscia", "lekow-z-alkoholem")
        Dim sBaseUri As String = "https://ktomalek.pl/l/interakcje/"

        Dim oResp As Net.Http.HttpResponseMessage = Nothing
        Dim sResp As String = ""
        oResp = Await moHttp.GetAsync(New Uri(sBaseUri & sLink & "/a/br-" & sId)) ' dla substancji: https://ktomalek.pl/l/interakcje/pomiedzy-lekami/bisoprololum/su-2210
        sResp = Await oResp.Content.ReadAsStringAsync
        Await Task.Delay(100)   ' poczekajmy

        Dim sMsg As String = ""

        If sResp.Contains("Nie posiadamy informacji wskazujących") Then
            sMsg = "Brak interakcji"
        Else
            ' Interakcja<br/>bardzo istotna
            ' <img loading="lazy" class="bgr-c:orange" alt="Alkohol" src="https://cdn.osoz.pl/kml/common/themes/images/leki-interakcje-alkohol.png">
            ' Interakcja<br/>istotna
            ' <img loading="lazy" class="bgr-c:red" alt="Alkohol" src="https://cdn.osoz.pl/kml/common/themes/images/leki-interakcje-alkohol.png">

            Dim iInd As Integer = sResp.IndexOf("Interakcja<br/>")
            If iInd > 1 Then
                sMsg = sResp.Substring(iInd + "Interakcja<br/>".Length, 20)
                iInd = sMsg.IndexOf("<")
                sMsg = sMsg.Substring(0, iInd)
            End If

        End If

        If bJedzenie Then
            sInterJedz = sMsg
        Else
            sInterAlk = sMsg
        End If

        Return sMsg
    End Function

End Class
