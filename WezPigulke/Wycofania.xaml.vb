﻿Imports pkar.UI.Extensions

Public NotInheritable Class Wycofania
    Inherits Page

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        Me.GoBack
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        If vblib.globalsy.glWycofaniaGIF Is Nothing Then
            App.InitLoadDecyzje()
        End If

        uiLista.ItemsSource = vblib.globalsy.glWycofaniaGIF.GetAll.OrderByDescending(Of String)(Function(c) c.data).ToList()
    End Sub

    Private Sub uiDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        Dim oItem As vblib.WycofaniaGIF.ImportowanaDecyzja = TryCast(oFE?.DataContext, vblib.WycofaniaGIF.ImportowanaDecyzja)

        If oItem Is Nothing Then Return

        Dim urik As New Uri($"https://rdg.ezdrowie.gov.pl/Decision/Decision?id={oItem.nr}")
        urik.OpenBrowser
    End Sub
End Class

Public Class KonwerterSerii
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim lista As List(Of vblib.WycofaniaGIF.ImportowanaDecyzja_Seria) = TryCast(value, List(Of vblib.WycofaniaGIF.ImportowanaDecyzja_Seria))

        If lista Is Nothing Then Return ""

        Dim sb As New System.Text.StringBuilder
        For Each item As vblib.WycofaniaGIF.ImportowanaDecyzja_Seria In lista
            sb.Append($"{item.nr} ({item.datawazn}), ")
        Next

        Return sb.ToString().TrimEnd(", ")
    End Function
End Class
