﻿

#pragma checksum "C:\Users\James\Documents\Visual Studio 11\Projects\VideoPlayer\VideoPlayer\BlankPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "22CA5D99874CA2F6A77BE4214AE45CB6"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace VideoPlayer
{
    public partial class BlankPage : Windows.UI.Xaml.Controls.Page
    {
        private Windows.UI.Xaml.Controls.MediaElement myMediaElement;
        private Windows.UI.Xaml.Controls.Button OpenFileBtn;
        private bool _contentLoaded;

        [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent()
        {
            if (_contentLoaded)
                return;

            _contentLoaded = true;
            Application.LoadComponent(this, new System.Uri("ms-appx:///BlankPage.xaml"), Windows.UI.Xaml.Controls.Primitives.ComponentResourceLocation.Application);
 
            myMediaElement = (Windows.UI.Xaml.Controls.MediaElement)this.FindName("myMediaElement");
            OpenFileBtn = (Windows.UI.Xaml.Controls.Button)this.FindName("OpenFileBtn");
        }
    }
}



