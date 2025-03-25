using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CopyMonitor
{
    public class CopyMonitorLevels : IExternalCommand
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
                foreach (var linkInstance in linkInstances)
                {
                    Document linkedDoc = linkInstance.GetLinkDocument();
                    var linkedLevels = new FilteredElementCollector(linkedDoc).OfClass(typeof(Level))
                                                                        .Cast<Level>().ToList();
                    if (!linkedLevels.Any())
                    {
                        TaskDialog.Show("Error", "No levels found in the linked model.");
                        return Result.Failed;
                    }

                    Dictionary<string, Level> existingLevels = new FilteredElementCollector(doc)
                                 .OfClass(typeof(Level)).Cast<Level>().ToDictionary(l => l.Name, l => l);

                    using (Transaction tx = new Transaction(doc, "Copy Monitor Levels"))
                    {
                        tx.Start();

                        foreach (var level in existingLevels)
                        {

                        }

                        tx.Commit();
                    }

                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
