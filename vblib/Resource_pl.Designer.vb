﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.42000
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System
Imports System.Reflection

Namespace My.Resources
    
    'This class was auto-generated by the StronglyTypedResourceBuilder
    'class via a tool like ResGen or Visual Studio.
    'To add or remove a member, edit your .ResX file then rerun ResGen
    'with the /str option, or rebuild your VS project.
    '''<summary>
    '''  A strongly-typed resource class, for looking up localized strings, etc.
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0"),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
    Friend Class Resource_PL
        
        Private Shared resourceMan As Global.System.Resources.ResourceManager
        
        Private Shared resourceCulture As Global.System.Globalization.CultureInfo
        
        <Global.System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")>  _
        Friend Sub New()
            MyBase.New
        End Sub
        
        '''<summary>
        '''  Returns the cached ResourceManager instance used by this class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Shared ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
            Get
                If Object.ReferenceEquals(resourceMan, Nothing) Then
                    Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("vblib.Resource_PL", GetType(Resource_PL).GetTypeInfo.Assembly)
                    resourceMan = temp
                End If
                Return resourceMan
            End Get
        End Property
        
        '''<summary>
        '''  Overrides the current thread's CurrentUICulture property for all
        '''  resource lookups using this strongly typed resource class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Shared Property Culture() As Global.System.Globalization.CultureInfo
            Get
                Return resourceCulture
            End Get
            Set
                resourceCulture = value
            End Set
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to PL.
        '''</summary>
        Friend Shared ReadOnly Property _lang() As String
            Get
                Return ResourceManager.GetString("_lang", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to BŁĄD: nie mogę otworzyć folderu roam?.
        '''</summary>
        Friend Shared ReadOnly Property errNoRoamFolder() As String
            Get
                Return ResourceManager.GetString("errNoRoamFolder", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Data modified both locally and in cloud, choose what version I should use.
        '''</summary>
        Friend Shared ReadOnly Property msgConflictModifiedODandLocal() As String
            Get
                Return ResourceManager.GetString("msgConflictModifiedODandLocal", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Local.
        '''</summary>
        Friend Shared ReadOnly Property msgConflictUseLocal() As String
            Get
                Return ResourceManager.GetString("msgConflictUseLocal", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Cloud.
        '''</summary>
        Friend Shared ReadOnly Property msgConflictUseOD() As String
            Get
                Return ResourceManager.GetString("msgConflictUseOD", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Pusty plik z przypomnieniami (lub błąd wczytania) - zainicjalizować go?.
        '''</summary>
        Friend Shared ReadOnly Property msgEmptyReminders() As String
            Get
                Return ResourceManager.GetString("msgEmptyReminders", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to 0/d, pauza.
        '''</summary>
        Friend Shared ReadOnly Property msgFreq0() As String
            Get
                Return ResourceManager.GetString("msgFreq0", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to 1/d, raz, o.
        '''</summary>
        Friend Shared ReadOnly Property msgFreq1() As String
            Get
                Return ResourceManager.GetString("msgFreq1", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to 2/d, rano i wieczór, 9/21.
        '''</summary>
        Friend Shared ReadOnly Property msgFreq2() As String
            Get
                Return ResourceManager.GetString("msgFreq2", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to 3/d, co 8 godzin, 8/16/23.
        '''</summary>
        Friend Shared ReadOnly Property msgFreq3() As String
            Get
                Return ResourceManager.GetString("msgFreq3", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Pomimo wyłączenia na tym device?.
        '''</summary>
        Friend Shared ReadOnly Property msgPomimoWylaczenia() As String
            Get
                Return ResourceManager.GetString("msgPomimoWylaczenia", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Toasty wygenerowane.
        '''</summary>
        Friend Shared ReadOnly Property msgRescheduleDone() As String
            Get
                Return ResourceManager.GetString("msgRescheduleDone", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Włączyłeś Toasty - chcesz je wygenerować?.
        '''</summary>
        Friend Shared ReadOnly Property msgShouldCreateToast() As String
            Get
                Return ResourceManager.GetString("msgShouldCreateToast", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Wyłączyłeś Toasty - chcesz je usunąć?.
        '''</summary>
        Friend Shared ReadOnly Property msgShouldRemoveToast() As String
            Get
                Return ResourceManager.GetString("msgShouldRemoveToast", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Po.
        '''</summary>
        Friend Shared ReadOnly Property resAfterButton() As String
            Get
                Return ResourceManager.GetString("resAfterButton", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Po zdarzeniu.
        '''</summary>
        Friend Shared ReadOnly Property resAfterList() As String
            Get
                Return ResourceManager.GetString("resAfterList", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Przed.
        '''</summary>
        Friend Shared ReadOnly Property resBeforeButton() As String
            Get
                Return ResourceManager.GetString("resBeforeButton", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Tuż przed zdarzeniem.
        '''</summary>
        Friend Shared ReadOnly Property resBeforeList() As String
            Get
                Return ResourceManager.GetString("resBeforeList", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Poniechaj.
        '''</summary>
        Friend Shared ReadOnly Property resDlgCancel() As String
            Get
                Return ResourceManager.GetString("resDlgCancel", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Kontynuuj.
        '''</summary>
        Friend Shared ReadOnly Property resDlgContinue() As String
            Get
                Return ResourceManager.GetString("resDlgContinue", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Nie.
        '''</summary>
        Friend Shared ReadOnly Property resDlgNo() As String
            Get
                Return ResourceManager.GetString("resDlgNo", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Tak.
        '''</summary>
        Friend Shared ReadOnly Property resDlgYes() As String
            Get
                Return ResourceManager.GetString("resDlgYes", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Dom.
        '''</summary>
        Friend Shared ReadOnly Property resHomeButton() As String
            Get
                Return ResourceManager.GetString("resHomeButton", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to W domu.
        '''</summary>
        Friend Shared ReadOnly Property resHomeList() As String
            Get
                Return ResourceManager.GetString("resHomeList", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Normal.
        '''</summary>
        Friend Shared ReadOnly Property resIgnoreButton() As String
            Get
                Return ResourceManager.GetString("resIgnoreButton", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Normalna pora.
        '''</summary>
        Friend Shared ReadOnly Property resIgnoreList() As String
            Get
                Return ResourceManager.GetString("resIgnoreList", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Później.
        '''</summary>
        Friend Shared ReadOnly Property resPillDelay() As String
            Get
                Return ResourceManager.GetString("resPillDelay", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Zażyłem!.
        '''</summary>
        Friend Shared ReadOnly Property resPillTaken() As String
            Get
                Return ResourceManager.GetString("resPillTaken", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to Pigułkowe larum.
        '''</summary>
        Friend Shared ReadOnly Property resToastTitle() As String
            Get
                Return ResourceManager.GetString("resToastTitle", resourceCulture)
            End Get
        End Property
    End Class
End Namespace
