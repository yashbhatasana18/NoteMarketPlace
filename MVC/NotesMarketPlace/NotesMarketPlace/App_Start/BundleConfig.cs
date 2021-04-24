using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;

namespace NotesMarketPlace.App_Start
{
    public class BundleConfig
    {
        public static void RegisterBundle(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/JqueryBundles/js").Include(
                "~/Scripts/jquery-{version}.min.js",
                "~/Scripts/jquery.validate.min.js",
                "~/Scripts/jquery.validate.unobtrusive.min.js"));

            bundles.Add(new ScriptBundle("~/ModernizrBundles/js").Include(
                "~/Scripts/modernizr-2.8.3.js"));

            bundles.Add(new ScriptBundle("~/PopperBundles/js").Include(
                "~/Scripts/popper.min.js"));

            bundles.Add(new ScriptBundle("~/BootstrapBundles/js").Include(
               "~/Scripts/bootstrap.min.js"));

            bundles.Add(new ScriptBundle("~/FrontPanelBundles/js").Include(
                "~/Content/FrontPanel/js/upload.js",
                "~/Content/FrontPanel/js/script.js"));

            bundles.Add(new StyleBundle("~/content/css").Include(
                "~/Content/bootstrap.min.css",
                "~/Content/icons-1.4.0/font/bootstrap-icons.css",
                "~/Content/FrontPanel/css/upload.css",
                "~/Content/FrontPanel/css/style.css",
                "~/Content/FrontPanel/css/responsive.css"));

            BundleTable.EnableOptimizations = true;
        }
    }
}