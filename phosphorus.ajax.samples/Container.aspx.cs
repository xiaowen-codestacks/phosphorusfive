/*
 * phosphorus five, copyright 2014 - thomas@magixilluminate.com
 * phosphorus five is licensed as mitx11, see the enclosed readme.me file for details
 */

namespace phosphorus.ajax.samples
{
    using System;
    using System.Web;
    using System.Web.UI;
    using phosphorus.ajax.core;
    using pf = phosphorus.ajax.widgets;

    public partial class Container : AjaxPage
    {
        // using the same event handler for both of our literal widgets
        protected void element_onclick (pf.Literal literal, EventArgs e)
        {
            literal.innerHTML = "widget was clicked";
        }
    }
}

