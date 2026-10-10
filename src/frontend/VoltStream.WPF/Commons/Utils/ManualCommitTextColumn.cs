namespace VoltStream.WPF.Commons.Utils;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

// DataGridTextColumn bir tomonlama bog'lanishda IsReadOnly'ni majburan true qiladi, ikki tomonlamasi esa matnni
// CellEditEnding tekshirishidan oldin yozib yuboradi — shuning uchun qiymatni sahifaning o'zi qo'llaydi.
public class ManualCommitTextColumn : DataGridTemplateColumn
{
    public BindingBase Binding { get; set; } = null!;
    public Style? ElementStyle { get; set; }
    public Style? EditingElementStyle { get; set; }

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        var text = new TextBlock { Style = ElementStyle };
        BindingOperations.SetBinding(text, TextBlock.TextProperty, Binding);
        return text;
    }

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
    {
        var box = new TextBox { Style = EditingElementStyle };
        BindingOperations.SetBinding(box, TextBox.TextProperty, Binding);
        return box;
    }
}
