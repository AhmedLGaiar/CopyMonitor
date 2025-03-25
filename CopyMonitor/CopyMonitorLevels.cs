using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitLink
{
    [Transaction(TransactionMode.Manual)]
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

                using (Transaction tx = new Transaction(doc, "Copy Monitor Levels"))
                {

                    foreach (var linkInstance in linkInstances)
                    {
                        Document linkedDoc = linkInstance.GetLinkDocument();

                        List<Level> linkedLevels = new FilteredElementCollector(linkedDoc).OfClass(typeof(Level))
                                                                            .Cast<Level>().ToList();
                        if (!linkedLevels.Any())
                        {
                            TaskDialog.Show("Error", "No levels found in the linked model.");
                            return Result.Failed;
                        }

                        {
                            tx.Start();

                            foreach (var linkedLevel in linkedLevels)
                            {

                                string linkedLevelName = linkedLevel.Name;
                                double linkedElevation = linkedLevel.Elevation;

                                // Create a new level
                                Level newLevel = Level.Create(doc, linkedElevation);
                                newLevel.Name = linkedLevelName;

                                TaskDialog.Show("Level Created", $"New Level: {linkedLevelName} at {linkedElevation} ft");

                                // Create associated floor and ceiling plans
                                CreatePlanViews(doc, newLevel);

                            }
                            tx.Commit();
                        }

                    }
                    return Result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        private void CreatePlanViews(Document doc, Level level)
        {
            // Create Floor Plan
            ViewFamilyType floorPlanType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                                    .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

            string floorPlanName = $"{level.Name} - Floor Plan";
            if (floorPlanType != null)
            {
                ViewPlan floorPlan = ViewPlan.Create(doc, floorPlanType.Id, level.Id);
                floorPlan.Name = floorPlanName;
            }

            // Create Ceiling Plan
            ViewFamilyType ceilingPlanType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.CeilingPlan);

            string ceilingPlanName = $"{level.Name} - Ceiling Plan";
            if (ceilingPlanType != null)
            {
                ViewPlan ceilingPlan = ViewPlan.Create(doc, ceilingPlanType.Id, level.Id);
                ceilingPlan.Name = ceilingPlanName;
            }
        }
    }
}

