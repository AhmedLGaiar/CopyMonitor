using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitLink
{
    [Transaction(TransactionMode.Manual)]
    public class ApplyHalftone : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                List<RevitLinkInstance> linkInstances = new FilteredElementCollector(doc)
                                    .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().ToList();

                if (!linkInstances.Any())
                {
                    TaskDialog.Show("Error", "No linked models found.");
                    return Result.Failed;
                }

                var validViewTypes = new HashSet<ViewFamily>
                {
                    ViewFamily.FloorPlan,
                    ViewFamily.CeilingPlan,
                    ViewFamily.Section,
                    ViewFamily.Elevation,
                    ViewFamily.ThreeDimensional
                };

                // Get all views except templates
                var allViews = new FilteredElementCollector(doc)
                    .OfClass(typeof(View)).Cast<View>()
                    .Where(v => !v.IsTemplate && validViewTypes.Contains(GetViewFamily(doc, v))).ToList();

                if (!allViews.Any())
                {
                    TaskDialog.Show("Error", "No valid views found.");
                    return Result.Failed;
                }

                using (Transaction tx = new Transaction(doc, "Apply Half tone"))
                {
                    tx.Start();

                    // Create halftone override settings
                    OverrideGraphicSettings overrideGraphic = new OverrideGraphicSettings();
                    overrideGraphic.SetHalftone(true);

                    // Apply halftone override in all views
                    foreach (var view in allViews)
                    {
                        foreach (var linkInstance in linkInstances)
                        {
                            view.SetElementOverrides(linkInstance.Id, overrideGraphic);
                        }
                    }

                    tx.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        private ViewFamily GetViewFamily(Document doc, View view)
        {
            ViewFamilyType viewFamilyType = doc.GetElement(view.GetTypeId()) as ViewFamilyType;
            return viewFamilyType != null ? viewFamilyType.ViewFamily : ViewFamily.Invalid;
        }
    }
}