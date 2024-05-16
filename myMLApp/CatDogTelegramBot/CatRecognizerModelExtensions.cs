namespace CatDogTelegramBot
{
    public partial class CatRecognizerModel
    {
        public static Task<ModelOutput> PredictAsync(ModelInput input)
        {
            return Task.Run(() => Predict(input));
        }
        public static IOrderedEnumerable<KeyValuePair<string, float>> processPictureWithModel(string filepath)
        {
            var imageBytes = System.IO.File.ReadAllBytes(filepath);
            CatRecognizerModel.ModelInput sampleData = new CatRecognizerModel.ModelInput()
            {
                ImageSource = imageBytes,
            };
            return CatRecognizerModel.PredictAllLabels(sampleData);
        }
    }
}
