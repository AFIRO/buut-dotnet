using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Rise.Client.Components.Table;

/// <summary>
/// A Service to create tables with preformed styling
/// </summary>
public static class TableCellService
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="cssClass"></param>
    /// <returns></returns>
    public static RenderFragment DefaultTableCell(string value, string cssClass = "") => builder =>
    {
        builder.OpenElement(0, "td");
        builder.AddAttribute(1, "class", $"text-xs font-weight-bold mb-0 {cssClass}");
        builder.AddContent(2, value);
        builder.CloseElement();
    };


    /// <summary>
    /// Creation method for badges
    /// </summary>
    /// <param name="value"></param>
    /// <param name="badgeType"></param>
    /// <returns>RenderFragment</returns>
    public static RenderFragment BadgeCell(string value, string badgeType = "bg-gradient-secondary") => builder =>
    {
        builder.OpenElement(0, "td");
        builder.OpenElement(1, "span");
        builder.AddAttribute(2, "class", $"badge w-100 {badgeType}");
        builder.AddContent(3, value);
        builder.CloseElement();
        builder.CloseElement();
    };        
    
    public static RenderFragment ParagraphCell(IEnumerable<LocalizedString?> values) => builder =>
    {
        builder.OpenElement(0, "td");

        foreach (var value in values)
        {
            builder.OpenElement(1, "p");
            builder.AddAttribute(2, "class", "text-xs font-weight-bold mb-0");
            builder.AddContent(3, value);
            builder.CloseElement(); 
        }

        builder.CloseElement();
    };    
    
    public static RenderFragment UserCell(string firstName = "", string lastName = "", string contact = "") => builder =>
    {
        builder.OpenElement(0, "td");
        builder.AddAttribute(1, "class", "align-middle ");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "d-flex py-1");

        // Icon
        builder.OpenElement(4, "div");
        builder.OpenElement(5, "i");
        builder.AddAttribute(6, "class", "ni ni-single-02 p-2");
        builder.CloseElement();
        builder.CloseElement();

        // Contact information
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "d-flex flex-column justify-content-center");

        // Name
        builder.OpenElement(9, "h6");
        builder.AddAttribute(10, "class", "mb-0 text-xs");
        builder.AddContent(11, $"{firstName} {lastName}");
        builder.CloseElement();

        // Phone Number
        builder.OpenElement(12, "p");
        builder.AddAttribute(13, "class", "text-xs text-secondary mb-0");
        builder.AddContent(14, contact);
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    };

        public static RenderFragment ActionCell(string id, object receiver, Func<string, Task>? editCallback, Func<string, Task>? deleteCallback) => builder =>
    {
        
        builder.OpenElement(0, "td");
        builder.OpenElement(1, "div");
        builder.AddAttribute(2, "style", "width: 64px;");
        if (editCallback != null)
        {
            // Edit action
            builder.OpenElement(3, "a");
            builder.AddAttribute(4, "class", "text-secondary font-weight-bold text-xs mr-3");
            builder.AddAttribute(5, "data-toggle", "tooltip");
            builder.AddAttribute(6, "data-original-title", "Edit user");
            builder.AddAttribute(7, "style", "cursor: pointer;");
            builder.AddAttribute(8, "onclick", EventCallback.Factory.Create(receiver, () => editCallback(id)));

            builder.OpenElement(9, "svg");
            builder.AddAttribute(10, "style", "width: 16px; height: 16px;");
            builder.AddAttribute(11, "xmlns", "http://www.w3.org/2000/svg");
            builder.AddAttribute(12, "viewBox", "0 0 512 512");
            builder.AddMarkupContent(13,
                "<path d='M471.6 21.7c-21.9-21.9-57.3-21.9-79.2 0L362.3 51.7l97.9 97.9 30.1-30.1c21.9-21.9 21.9-57.3 0-79.2L471.6 21.7zm-299.2 220c-6.1 6.1-10.8 13.6-13.5 21.9l-29.6 88.8c-2.9 8.6-.6 18.1 5.8 24.6s15.9 8.7 24.6 5.8l88.8-29.6c8.2-2.7 15.7-7.4 21.9-13.5L437.7 172.3 339.7 74.3 172.4 241.7zM96 64C43 64 0 107 0 160L0 416c0 53 43 96 96 96l256 0c53 0 96-43 96-96l0-96c0-17.7-14.3-32-32-32s-32 14.3-32 32l0 96c0 17.7-14.3 32-32 32L96 448c-17.7 0-32-14.3-32-32l0-256c0-17.7 14.3-32 32-32l96 0c17.7 0 32-14.3 32-32s-14.3-32-32-32L96 64z' />");
            builder.CloseElement();
            builder.CloseElement();
        }

        if (deleteCallback != null)
        {
           // Delete action
           builder.OpenElement(14, "a");
           builder.AddAttribute(15, "class", "text-secondary font-weight-bold text-xs");
           builder.AddAttribute(16, "data-toggle", "tooltip");
           builder.AddAttribute(17, "data-original-title", "Delete user");
           builder.AddAttribute(18, "style", "cursor: pointer;");
           builder.AddAttribute(19, "onclick", EventCallback.Factory.Create(receiver, () => deleteCallback(id))); 
                      builder.OpenElement(20, "svg");
           builder.AddAttribute(21, "style", "width: 16px; height: 16px; fill: darkred;"); 
           builder.AddAttribute(22, "xmlns", "http://www.w3.org/2000/svg");
           builder.AddAttribute(23, "viewBox", "0 0 448 512");
           builder.AddMarkupContent(24, "<path d='M135.2 17.7L128 32 32 32C14.3 32 0 46.3 0 64S14.3 96 32 96l384 0c17.7 0 32-14.3 32-32s-14.3-32-32-32l-96 0-7.2-14.3C307.4 6.8 296.3 0 284.2 0L163.8 0c-12.1 0-23.2 6.8-28.6 17.7zM416 128L32 128 53.2 467c1.6 25.3 22.6 45 47.9 45l245.8 0c25.3 0 46.3-19.7 47.9-45L416 128z' />");
           builder.CloseElement();
           builder.CloseElement();
        }
        
        builder.CloseElement();
        builder.CloseElement();
    };
}