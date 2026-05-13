using DevStation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace DevStation.Views.Pages;

public partial class DocumentationPage : UserControl
{
    public DocumentationPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not DocumentationViewModel vm) return;

        vm.ScrollToSection = sectionKey =>
        {
            var elementName = sectionKey switch
            {
                "getting-started" => "sectionGettingStarted",
                "svg-converter"   => "sectionSvgConverter",
                "image-optimizer" => "sectionImageOptimizer",
                "mdn-search"      => "sectionMdnSearch",
                "mock-generator"  => "sectionMockGenerator",
                "snippets"        => "sectionSnippets",
                "faq"             => "sectionFaq",
                _                 => null
            };

            if (elementName == null) return;
            if (FindName(elementName) is not FrameworkElement element) return;

            try
            {
                var transform = element.TransformToAncestor(MainScrollViewer);
                var position  = transform.Transform(new Point(0, 0));
                MainScrollViewer.ScrollToVerticalOffset(
                    MainScrollViewer.VerticalOffset + position.Y - 32);
            }
            catch
            {
                element.BringIntoView();
            }
        };
    }
}
