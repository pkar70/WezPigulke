
' to, o czym przypominamy
Public Class JedenZestaw
    <Xml.Serialization.XmlAttribute>
    Public Property sId As String
    <Xml.Serialization.XmlAttribute>
    Public Property sNazwaZestawu As String
    <Xml.Serialization.XmlAttribute>
    Public Property sTakeTimes As String
    <Xml.Serialization.XmlAttribute>
    Public Property sMelodyjka As String
    <Xml.Serialization.XmlAttribute>
    Public Property sNextOrgTime As String     ' taki jaki wynika z scheduler (bez 'snooze')
    <Xml.Serialization.XmlAttribute>
    Public Property iDelayMins As Integer   ' do tego dodawany jest 'snooze'
    <Xml.Serialization.XmlAttribute>
    Public Property bEnabled As Boolean

    <Xml.Serialization.XmlIgnore>
    Public Property oNextOrgTime As DateTimeOffset = New DateTime(9001, 12, 31)   ' poza sortowaniem
    <Xml.Serialization.XmlIgnore>
    Public Property oNextTime As DateTimeOffset = New DateTime(9001, 12, 31)
    <Xml.Serialization.XmlIgnore>
    Public Property sDisplayTime As String = ""
    <Xml.Serialization.XmlIgnore>
    Public Property sDisplayOrgTime As String = ""
    <Xml.Serialization.XmlIgnore>
    Public Property bIsThisMoje As Visibility
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
    Public Property bWycofane As Boolean
    Public Property sOpakowania As String
    Public Property sDawneOpakowania As String
    Public Property iTypLeku As Integer = 0   ' 0-chwilowy, 1-dłuższy, 2-stały (do interakcji)
    <Xml.Serialization.XmlIgnore>
    Public Property bIncludeInteraction As Boolean = False
    <Xml.Serialization.XmlIgnore>
    Public Property bStaly As Boolean = False
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
End Class