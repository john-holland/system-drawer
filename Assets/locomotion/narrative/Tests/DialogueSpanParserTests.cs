using NUnit.Framework;
using Locomotion.Narrative;

namespace Locomotion.Narrative.Tests
{
    public class DialogueSpanParserTests
    {
        const string BookConcert = @"
{P:dialogue|dialogue-set=book-concert}""What books do you think should play?""
{P:dialogue|answer=windy-man|speaker=fox}""The windy man.""
{P:dialogue|answer=long-mover|speaker=fox}""The Long Mover: The Python""
  {P:dialogue|answer=handcuff-python|speaker=prince}""Oh, handcuff the python!?""
{P:dialogue|end-block=book-concert}";

        [Test]
        public void Compile_BookConcert_HasRootAndChildren()
        {
            var result = DialogueSpanParser.Compile(BookConcert, "book-concert");
            Assert.AreEqual("book-concert", result.setId);
            Assert.GreaterOrEqual(result.nodes.Count, 1);
            Assert.GreaterOrEqual(result.nodes[0].children.Count, 1);
        }

        [Test]
        public void Compile_ParsesSpeakerKey()
        {
            var result = DialogueSpanParser.Compile(BookConcert, "book-concert");
            Assert.AreEqual("fox", result.nodes[0].children[0].speakerKey);
        }

        [Test]
        public void ParseVisMode_MapsAliases()
        {
            Assert.AreEqual(SpeechVisMode.ScaleWobble, ActorSpeechPlayback.ParseVisMode("wobble"));
            Assert.AreEqual(SpeechVisMode.Jaw, ActorSpeechPlayback.ParseVisMode("jaw"));
        }
    }
}
