﻿#pragma checksum "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "9C02331442B5551FB6803A34AA7D7705EBBDB9691E1B4B5336FA11F741492A79"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Xceed.Wpf.Toolkit;
using qgrepControls.ToolWindows;


namespace qgrepControls.ToolWindows {
    
    
    /// <summary>
    /// RuleRow
    /// </summary>
    public partial class RuleRow : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 14 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid RuleGrid;
        
        #line default
        #line hidden
        
        
        #line 16 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label RuleInclude;
        
        #line default
        #line hidden
        
        
        #line 17 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label RuleContent;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel Icons;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button DeleteButton;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/qgrepControls;component/toolwindows/projectswindow/rulerow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.RuleGrid = ((System.Windows.Controls.Grid)(target));
            
            #line 14 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
            this.RuleGrid.MouseEnter += new System.Windows.Input.MouseEventHandler(this.RuleGrid_MouseEnter);
            
            #line default
            #line hidden
            
            #line 14 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
            this.RuleGrid.MouseLeave += new System.Windows.Input.MouseEventHandler(this.RuleGrid_MouseLeave);
            
            #line default
            #line hidden
            
            #line 14 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
            this.RuleGrid.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.RuleGrid_MouseDown);
            
            #line default
            #line hidden
            return;
            case 2:
            this.RuleInclude = ((System.Windows.Controls.Label)(target));
            return;
            case 3:
            this.RuleContent = ((System.Windows.Controls.Label)(target));
            return;
            case 4:
            this.Icons = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 5:
            this.DeleteButton = ((System.Windows.Controls.Button)(target));
            
            #line 20 "..\..\..\..\ToolWindows\ProjectsWindow\RuleRow.xaml"
            this.DeleteButton.Click += new System.Windows.RoutedEventHandler(this.DeleteButton_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

