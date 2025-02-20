Imports pkar.UI.Extensions

Public Class UCprocedura
    Inherits TextBox

    Public Sub New()
        MyBase.New

        Me.IsReadOnly = True
        Me.BorderThickness = New Thickness(0)

        ' pierwsze na zmianę text z kodu, ale nie wywoływane jest via Bindings
        AddHandler Me.TextChanged, AddressOf UstawTooltip
        ' drugie by złapać zmianę w bindings
        AddHandler Me.DataContextChanged, AddressOf UstawTooltipDC

        DodajContextMenu()
    End Sub



    Public Shared Sub ShowProceduryWeb()
        Dim oUri As New Uri("https://getmedi.pl/news/25/procedury-rejestracyjne-wsrod-lekow-refundowanych")
        oUri.OpenBrowser
    End Sub


    Private Sub DodajContextMenu()

        Dim meni As New MenuFlyout

        Dim miDlg As New MenuFlyoutItem() With {.Text = "opis"}
        miDlg.DataContext = Me
        AddHandler miDlg.Click, AddressOf uiShowDlgBox_Click

        meni.Items.Add(miDlg)

        Dim miWeb As New MenuFlyoutItem() With {.Text = "web 🌍"}
        AddHandler miWeb.Click, AddressOf uiShowProcedury_Click

        meni.Items.Add(miWeb)

        Me.ContextFlyout = meni
    End Sub

    Private Sub uiShowProcedury_Click(sender As Object, e As RoutedEventArgs)
        ShowProceduryWeb()
    End Sub


    Private Sub uiShowDlgBox_Click(sender As Object, e As RoutedEventArgs)
        'Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        'If oFE Is Nothing Then Return

        'Dim proc As String = CType(oFE.DataContext, String)

        Dim msg As String
        Select Case Me.Text.ToLowerInvariant
            Case "cen"
                msg =
                    $"W procedurze centralnej rejestracji wydawane jest pozwolenie na dopuszczenie do obrotu, które obowiązuje we wszystkich państwach członkowskich oraz na Islandii, w Liechtenstein i Norwegii. Takie pozwolenie wydaje Komisja Europejska, a za rozpatrzenie wniosku odpowiada Europejska Agencja Leków (European Medicines Agency - EMA).
Procedura centralna jest obowiązkowa m.in. w przypadku leków:
* stosowanych u ludzi w leczeniu HIV/AIDS, nowotworów złośliwych, cukrzycy, zaburzeń neurodegeneracyjnych, chorób autoimmunologicznych i innych dysfunkcjach immunologicznych, chorób wirusowych;
* wytwarzanych w procesach biotechnologicznych;
* stosowanych w terapii zaawansowanej, np. w terapii genowej;
* stosowanych w chorobach rzadkich tzw. leków sierocych."
            Case "nar"
                msg = "Procedura narodowa (NAR) prowadzona jest według przepisów mających zastosowanie w danym kraju. W Polsce dopuszczeniem leku do obrotu w procedurze narodowej zajmuje się Urząd Rejestracji Produktów Leczniczych, Wyrobów Medycznych i Produktów Biobójczych (URPL)."
            Case "mrp"
                msg = "Procedura MRP (z ang. Mutual Recognition Procedure) dotyczy produktów, które posiadają już pozwolenie na dopuszczenie do obrotu w jednym z krajów EU. Polega ona na rejestracji produktu w kolejnych krajach poprzez uznanie wydanego przez państwo referencyjne pozwolenia na dopuszczenie do obrotu."
            Case "dcp"
                msg = "Procedura DCP dotyczy leku jeszcze niezarejestrowanego w żadnym państwie członkowskim. Polega ona na rejestracji leku równolegle w kilku krajach EU. We wniosku o rejestrację musi zostać wskazany kraj, który będzie pełnił rolę państwa referencyjnego, odpowiedzialnego za wstępną ocenę i koordynację całego procesu."
            Case "ir"
                msg = "Procedura importu równoległego dotyczy sprowadzenia leków, które zostały wyprodukowane na rynek europejski inny niż polski. Leki te nie różnią ani jakością ani skutecznością terapeutyczną od leków przeznaczonych na rynek polski. Różnice dotyczą jedynie wyglądu opakowań. Pozwolenie na import równoległy jest wydawane na 5 lat."
            Case Else
                msg = "???"
        End Select


        vblib.MsgBox(msg)
    End Sub

    Private Sub UstawTooltip(sender As Object, e As TextChangedEventArgs)
        ToolTipService.SetToolTip(Me, GetTooltipFor(Me.Text))
    End Sub

    Private Sub UstawTooltipDC(sender As FrameworkElement, args As DataContextChangedEventArgs)
        ToolTipService.SetToolTip(Me, GetTooltipFor(Me.Text))
    End Sub

    Private Shared Function GetTooltipFor(proc As String) As String
        Select Case proc.ToLowerInvariant
            Case "cen"
                Return "CENtralna, ogólnounijna"
            Case "nar"
                Return "NARodowa (krajowa)"
            Case "mrp"
                Return "wzajemnego uznania (Mutual Recognition Procedure)"
            Case "dcp"
                Return "zdecentralizowana - kilka krajów"
            Case "ir"
                Return "import równoległy"
            Case Else
                Return "???"
        End Select
    End Function



End Class
