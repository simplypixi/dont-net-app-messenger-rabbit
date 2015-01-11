using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.DataGrid;
using System.Windows.Documents;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace DNAClient.ViewModel
{
    class DNAFormatter : ITextFormatter{
    public string GetText(System.Windows.Documents.FlowDocument document)
    {
      return new TextRange(document.ContentStart, document.ContentEnd).Text;
    }

    public void SetText(System.Windows.Documents.FlowDocument document, string text)
    {
      new TextRange(document.ContentStart, document.ContentEnd).Text = text;

     
      }

    }
}
