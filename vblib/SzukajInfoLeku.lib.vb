Imports pkar
Imports pkar.BaseStruct

Public Class SzukajInfoLeku

    'a.różne źródła
    '        i.z zestawu
    '        ii.z nazwy, z substancji
    '        iii.z barcode
    '        iv.z OCR barcode
    '    b.one są roam
    '    c.info z pub.rejestrymedyczne.csioz.gov.pl
    '    d.interakcje
    'e.dodaj lek stały, czasowy, sprawdź lek

    ' broncho-vaxom: 5909990062928 , ATC R07AX 
    ' ulotka: http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-u
    ' charakt: http://pub.rejestrymedyczne.csioz.gov.pl/Pobieranie.ashx?type=22265-c
    ' strona leku: http://pub.rejestrymedyczne.csioz.gov.pl/ProduktSzczegoly.aspx?id=22265

    ' szukanie: ATC code, czyli zastosowanie
    ' https://en.wikipedia.org/wiki/Anatomical_Therapeutic_Chemical_Classification_System
    ' z ATC jest dawkowanie :) https://www.whocc.no/atc_ddd_index/?code=C07AB07

    ' nazwa, common nazwa, substancja czynna, ale głównie EAN

    ' interakcje mogą być z substancji czynnej... o tyle dobrze :)

    ' mozna z pub.rejestry pojsc o jedna stronę dalej, i mieć:
    ' (active substancja) Szczepionka wieloważna
    ' Kod ATC - wtedy go rozpisac
    ' opakowania istniejące, z Rp - recepta
    ' opakowania wycofane

    ' mozna jakos robic cache danych (wedle EAN na przyklad)
    ' gdy GetSettingsBool("cacheDataHere")

    ' ListView: opcje: dodaj do zapamietanych (przechowywanych na dysku)

    ' ktomalek - mozna z nazwy szukac zamiennikow, sprawdzac w aptekach w ogóle! - ale bez cen


    ' https://rejestry.ezdrowie.gov.pl/registry/rpl - pełny rejestr ma 64 MB, aktualizacja dzienna - więc lepiej odpytywać serwer udając przeglądarkę


    Private Shared moHttp As New Net.Http.HttpClient


#Region "zamiana EAN/nazwa na listę leków"

    ' https://rejestry.ezdrowie.gov.pl/rpl/search/public
    '  curl 'https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/search/public?name=sobycor&subjectRolesIds=1&isAdvancedSearch=false&size=30&page=0&sort=name,ASC' \
    ' https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/search/public?eanGtin=5909991097295&subjectRolesIds=1&isAdvancedSearch=false&size=30&page=0&sort=name,ASC
    ' https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/search/public?name=soby&substanceName=biso&subjectRolesIds=1&isAdvancedSearch=false&size=30&page=0&sort=name,ASC
    ' https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/search/public/details/30714
    ' https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/search/public?atcCode=a10ba&subjectRolesIds=1&isAdvancedSearch=false&size=30&page=0&sort=name,ASC

    ' szukanie potem według ATCcode (jakby new query)


    Protected Shared Async Function ZapytajRejestrySpis(ean As String, nazwa As String, subst As String, atc As String) As Task(Of List(Of JSON_Lek_GlowneInfo))

        Dim sUri As String = ""

        If Not String.IsNullOrWhiteSpace(ean) Then
            sUri &= $"eanGtin={ean}&"
        End If

        If Not String.IsNullOrWhiteSpace(nazwa) Then
            sUri &= $"name={nazwa}&"
        End If

        If Not String.IsNullOrWhiteSpace(subst) Then
            sUri &= $"substanceName={subst}&"
        End If

        If Not String.IsNullOrWhiteSpace(atc) Then
            sUri &= $"atcCode={atc}&"
        End If

        ' nie było parametrów
        If sUri = "" Then Return Nothing

        ' page=30 to default value, ale chcę zakończyć & po parametrach
        sUri = "https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/search/public?" & sUri & "size=30"
        ' https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/search/public?name=sobyc&subjectRolesIds=1&isAdvancedSearch=false&size=30&page=0&sort=name,ASC
        Dim oUri As New Uri(sUri)

        Try
            Dim oResp As Net.Http.HttpResponseMessage = Await moHttp.GetAsync(oUri)
            Dim sResp As String = Await oResp.Content.ReadAsStringAsync

            ' If sEAN = "590999" Then sEAN = ""

            Dim tmp As New BaseList(Of JSON_SearchResult)(Nothing)
            Return tmp.LoadItem(sResp).content

        Catch ex As Exception
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' zwraca listę Pudełko wg parametrów szukania, najczęściej będzie to jedno pudełko (w odpowiedzi na EAN)
    ''' </summary>
    ''' <param name="ean"></param>
    ''' <param name="nazwa"></param>
    ''' <param name="subst"></param>
    ''' <param name="atc"></param>
    ''' <returns></returns>
    Public Shared Async Function GetListaPudelek(ean As String, nazwa As String, subst As String, atc As String) As Task(Of List(Of JednoPudelko))
        ' zamiana search na Collection(Of JednoPudelko) - do pokazywania

        Dim listalekow As List(Of JSON_Lek_GlowneInfo) = Await ZapytajRejestrySpis(ean, nazwa, subst, atc)

        If listalekow Is Nothing Then Return Nothing

        Dim ret As New List(Of JednoPudelko)
        For Each lek As JSON_Lek_GlowneInfo In listalekow
            ret.Add(lek.AsPudelko(ean))
        Next

        'Await UzupelnijOpakowania(ret)

        Return ret
    End Function

    ''' <summary>
    ''' do pudelko.opakowania dopisz wielkości (np. 10 pigulek, 20 pigulek...)
    ''' </summary>
    ''' <param name="pudelko"></param>
    ''' <returns></returns>
    Public Shared Async Function UzupelnijOpakowania(pudelko As JednoPudelko) As Task

        '' jeśli jest poprawny EAN/GTIN, to nic nie mamy do zrobienia
        'If pud.sBarcode.StartsWith("0590") AndAlso pud.sBarcode.Length = 14 Then Continue For
        'If pud.sBarcode.StartsWith("590") AndAlso pud.sBarcode.Length = 13 Then Continue For

        ' uzupełniamy EAN dla konkretnego leku
        Dim detale As JSON_Lek_Details = Await ZapytajRejestrySzczegoly(pudelko.IDrpl)
        If detale Is Nothing Then Return

        pudelko.sOpakowania = ""
        If detale.packages Is Nothing Then Return

        For Each opak In detale.packages
            pudelko.sOpakowania = pudelko.sOpakowania & $"{opak.packaging}: {opak.accessibilityCategory}, {opak.gtin}" & vbCrLf
        Next

        pudelko.sOpakowania = pudelko.sOpakowania.Trim

    End Function


#End Region


#Region "szczegóły leku"
    Private Shared Async Function ZapytajRejestrySzczegoly(idleku As Integer) As Task(Of JSON_Lek_Details)

        Dim sUri As String = "https://rejestry.ezdrowie.gov.pl/api/rpl/medicinal-products/search/public/details/" & idleku

        Dim oResp As Net.Http.HttpResponseMessage = Await moHttp.GetAsync(New Uri(sUri))
        Dim sResp As String = Await oResp.Content.ReadAsStringAsync

        ' If sEAN = "590999" Then sEAN = ""

        Dim tmp As New BaseList(Of JSON_Lek_Details)(Nothing)
        Dim ret = tmp.LoadItem(sResp)

        Return ret

    End Function
#End Region


#Region "structy szukania"

    Protected Class JSON_SearchResult
        Public Property content As List(Of JSON_Lek_GlowneInfo)
        Public Property pageable As JSON_SearchResult_Pageable
        Public Property totalPages As Integer ' 1
        Public Property totalElements As Integer ' 3
        Public Property last As Boolean ' true
        Public Property first As Boolean ' true
        Public Property numberOfElements As Integer ' 3
        Public Property sort As Object '[]
        Public Property number As Integer ' 0
        Public Property size As Integer ' 30
        Public Property empty As Boolean ' false
    End Class

    Protected Class JSON_SearchResult_Pageable
        Public Property pageNumber As Integer '0
        Public Property pageSize As Integer ' 30
        Public Property sort As Object ' []
        Public Property offset As Integer ' 0
        Public Property unpaged As Boolean ' false
        Public Property paged As Boolean ' true
    End Class

    Protected Class JSON_Lek_GlowneInfo
        Public Property id As Integer ' 30714
        Public Property specimenType As String ' "Ludzki", "Weterynaryjny"
        Public Property medicinalProductName As String ' "Sobycor"
        Public Property commonName As String ' "Bisoprololi fumaras"
        Public Property pharmaceuticalFormName As String ' "Tabletki powlekane", "Roztwór doustny"
        Public Property medicinalProductPower As String ' "2,5 mg", "2,5 g / 100 ml"
        Public Property activeSubstanceName As String ' "Bisoprololi fumaras"
        Public Property subjectMedicinalProductName As String ' "Krka, d.d., Novo mesto"
        Public Property registryNumber As String ' "21636"
        Public Property procedureTypeName As String ' "DCP"
        Public Property expirationDateString As String ' "Bezterminowe"
        Public Property atcCode As String ' "C07AB07"
        Public Property targetSpecies As String ' "" , "Indyk\nkura"

        ''' <summary>
        ''' zamień na JednoPudelko, używając podanego EAN
        ''' </summary>
        ''' <param name="ean"></param>
        ''' <returns></returns>
        Public Function AsPudelko(ean As String) As JednoPudelko
            Dim ret As New JednoPudelko

            ret.sBarcode = ean
            ret.sNazwa = medicinalProductName
            ret.sNazwaPowszechna = commonName
            ret.sMoc = medicinalProductPower
            ret.sPostac = pharmaceuticalFormName


            ret.sPozwolenie = registryNumber
            ret.sWaznosc = expirationDateString
            ret.sPodmiot = subjectMedicinalProductName
            ret.sProcedura = procedureTypeName


            'Public Property sDetailsLink As String TAK - (a) do wyciągniecia danych dokładnych, (b) do ściągania ulotek
            'Public Property sCreated As String

            ret.sNazwaCzynna = activeSubstanceName
            ret.sKodATC = atcCode

            'Public Property bWycofane As Boolean
            'Public Property sOpakowania As String TAK
            'Public Property sDawneOpakowania As String TAK
            'Public Property iTypLeku As Integer = 0   ' 0-chwilowy, 1-dłuższy, 2-stały (do interakcji)

            ret.IDrpl = id

            Return ret

        End Function
    End Class

#End Region

#Region "structy szczegoly leku"

    Protected Class JSON_Lek_Details
        Public Property gracePeriod As String ' ""
        Public Property characteristicFileName As Boolean ' true
        Public Property leafletFileName As Boolean ' true
        Public Property packageFileName As Boolean ' false
        Public Property packageLeafletAndLabellingFileName As Boolean ' false
        Public Property parallelImportLeafletFileName As Boolean ' false
        Public Property parallelImportPackageMarkingFileName As Boolean ' false
        Public Property parallelImportAdditionalDocumentOneFileName As Boolean ' false
        Public Property parallelImportAdditionalDocumentTwoFileName As Boolean ' false
        Public Property parallelImportPackageLeafletAndLabellingFileName As Boolean ' false
        Public Property decisionsAttachment As Boolean ' true
        Public Property rmpSummary As String ' "StreszczenieRmp_30714_Sobycor_21636.pdf"
        Public Property validVet As Boolean  ' false
        Public Property packages As List(Of JSON_Lek_Package)
        Public Property legalBasis As String ' "art. 15 ust. 1 pkt 2"
        Public Property producersOrImporters As List(Of JSON_Lek_ProdImporter)
    End Class

    Protected Class JSON_Lek_Package
        Public Property packageOrder As Integer ' 0 wszedzie było 0
        Public Property packaging As String ' np "56 tabl."
        Public Property accessibilityCategory As String ' "Rp"
        Public Property gtin As String ' "05909991097325"
    End Class

    Protected Class JSON_Lek_ProdImporter
        Public Property subjectName As String ' "Krka, d.d., Novo mesto"
        Public Property countryName As String ' "Słowenia"
    End Class

#End Region

End Class

