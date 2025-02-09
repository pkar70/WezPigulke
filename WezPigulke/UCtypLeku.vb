Imports Newtonsoft.Json.Linq
Imports vblib

Public Class UCtypLeku
    Inherits UserControl

    'Public Property Value As Integer
    '    Get
    '        Return _Slider.Value
    '    End Get
    '    Set(value As Integer)
    '        _Slider.Value = value

    '        Dim pud As JednoPudelko = TryCast(DataContext, JednoPudelko)
    '        If pud Is Nothing Then Return
    '        pud.iTypLeku = value
    '    End Set
    'End Property

    Private _Label As New TextBlock With {.HorizontalAlignment = HorizontalAlignment.Center, .FontSize = 8}
    Private _Slider As New Slider With {.Minimum = 0, .Maximum = 2, .Value = 1, .HorizontalAlignment = HorizontalAlignment.Stretch}
    Private _Border As New Border With {.HorizontalAlignment = HorizontalAlignment.Stretch, .VerticalAlignment = VerticalAlignment.Stretch}

    Private _TloApteczka As New SolidColorBrush(Windows.UI.Colors.LightGreen)
    Private _TloUzywany As New SolidColorBrush(Windows.UI.Colors.LightBlue)
    Private _TloError As New SolidColorBrush(Windows.UI.Colors.Red)
    'Application.Current.Resources("SystemControlBackgroundBaseLowBrush")
    'Application.Current.Resources("SystemControlForegroundBaseLowBrush") ' High daje pełny czarny
    'Private _TloArch = Application.Current.Resources("SystemControlForegroundBaseHighBrush")


    Public Event ValueChanged As RoutedEventHandler


    'Public Shared ReadOnly ValueProperty As DependencyProperty =
    'DependencyProperty.Register("Value",
    '  GetType(Integer), GetType(UCtypLeku),
    '  New PropertyMetadata(
    '    Nothing, New PropertyChangedCallback(AddressOf OnValueChanged)))

    'Private Shared Sub OnValueChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
    '    Dim kontrolka As UCtypLeku = CType(d, UCtypLeku) ' null checks omitted
    '    kontrolka.Value = CType(e.NewValue, Integer)
    'End Sub

    Public Sub New()
        MyBase.New

        Dim oGrid As New Grid
        oGrid.RowDefinitions.Clear()
        Dim rlabel As New RowDefinition
        rlabel.Height = New GridLength(1, GridUnitType.Auto)
        oGrid.RowDefinitions.Add(rlabel)
        rlabel = New RowDefinition
        rlabel.Height = New GridLength(1, GridUnitType.Auto)
        oGrid.RowDefinitions.Add(rlabel)

        AddHandler _Slider.ValueChanged, AddressOf SliderChanged

        Grid.SetRow(_Label, 0)
        Grid.SetRow(_Border, 1)

        _Border.Child = _Slider

        oGrid.Children.Add(_Label)
        oGrid.Children.Add(_Border)

        Me.Content = oGrid

        AddHandler Me.DataContextChanged, AddressOf KontekstZmiana

    End Sub

    Private _inZmianaKontekst As Boolean

    Private Sub KontekstZmiana(sender As FrameworkElement, args As DataContextChangedEventArgs)

        Dim pud As JednoPudelko = TryCast(args.NewValue, JednoPudelko)
        If pud Is Nothing Then Return

        _inZmianaKontekst = True
        _Slider.Value = pud.iTypLeku
        _inZmianaKontekst = False
    End Sub

    Private Sub SliderChanged(sender As Object, e As RangeBaseValueChangedEventArgs)
        ' koloru slidera nie da się zmienić?
        ' kolory Grid także są niezmienne (nie działają)

        'Debug.WriteLine("Slider zmieniam na: " & _Slider.Value)

        Dim pud As JednoPudelko = TryCast(DataContext, JednoPudelko)
        If pud Is Nothing Then Return
        pud.iTypLeku = _Slider.Value

        If Not _inZmianaKontekst Then
            'Debug.WriteLine("zmieniam na powaznie")
            RaiseEvent ValueChanged(Me, Nothing)
        End If

        Select Case _Slider.Value
            Case 0
                _Label.Text = "arch"
                _Border.Background = Me.Background
            Case 1
                _Label.Text = "apteczka"
                _Border.Background = _TloApteczka
                '_Slider.Foreground = _TloApteczka
                '_Label.Foreground = _TloApteczka
                'Me.Background = New SolidColorBrush(Windows.UI.Colors.Green) '_TloApteczka
            Case 2
                _Label.Text = "zażywany"
                _Border.Background = _TloUzywany
                '_Slider.Foreground = _TloUzywany
                '_Label.Foreground = _TloUzywany
                'Me.Background = _TloUzywany
                'Me.Foreground = _TloError
            Case Else
                _Label.Text = "??"
                _Border.Background = _TloError
                '_Slider.Foreground = _TloError
                '_Label.Foreground = _TloError
                'Me.Background = _TloError
        End Select
    End Sub
End Class
