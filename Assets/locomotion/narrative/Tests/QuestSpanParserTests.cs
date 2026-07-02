using NUnit.Framework;
using Locomotion.Narrative;

namespace Locomotion.Narrative.Tests
{
    public class QuestSpanParserTests
    {
        const string LittlePrince = @"
{P:quest|quest-set=little-prince-tour}""Explore the asteroid belt""
  {P:quest|objective=meet-fox|spatial4d=s4d-fox-vol|predicate4d=fox-met|completion4d=fox-dialogue-done}
    {P:quest|summary=Meet the fox on the equator|style=watercolor-storybook}
{P:quest|end-block=little-prince-tour}";

        [Test]
        public void Compile_LittlePrince_HasObjective()
        {
            var result = QuestSpanParser.Compile(LittlePrince, "little-prince-tour");
            Assert.AreEqual("little-prince-tour", result.setId);
            Assert.AreEqual("Explore the asteroid belt", result.title);
            Assert.GreaterOrEqual(result.nodes.Count, 1);
            var objective = result.nodes[0].children.Count > 0 ? result.nodes[0].children[0] : result.nodes[0];
            Assert.AreEqual("meet-fox", objective.objectiveId);
        }

        [Test]
        public void Compile_ParsesTravelBinding()
        {
            const string text = @"
{P:quest|quest-set=test}""Title""
  {P:quest|objective=o1|travel-binding=fox-approach|map-layer=emergence}
{P:quest|end-block=test}";
            var result = QuestSpanParser.Compile(text, "test");
            Assert.AreEqual("fox-approach", result.nodes[0].travelBinding);
            Assert.AreEqual("emergence", result.nodes[0].mapLayer);
        }
    }
}
