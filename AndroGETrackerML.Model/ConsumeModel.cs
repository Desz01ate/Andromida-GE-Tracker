// This file was auto-generated by ML.NET Model Builder. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ML;
using AndroGETrackerML.Model;
using AndroGETrackerML.Model.Enum;
using System.Text.RegularExpressions;

namespace AndroGETrackerML.Model
{
    public class ConsumeModel
    {
        // For more info on consuming ML.NET models, visit https://aka.ms/model-builder-consume
        // Method for consuming model in your app
        static MLContext mlContext = new MLContext();
        static PredictionEngine<ModelInput, ModelOutput> engine;
        const string modelPath = @"MLModel.zip";
        static ConsumeModel()
        {
            ITransformer mlModel = mlContext.Model.Load(modelPath, out var modelInputSchema);
            var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
            engine = predEngine;
        }
        public static ModelOutput Predict(ModelInput input)
        {
            ModelOutput result = engine.Predict(input);
            return result;
        }
        public static MessageType Predict(string input)
        {
            var cleanMessage = Regex.Replace(input, @"(- \[(.*?)\])", "", RegexOptions.Compiled);
            var result = Predict(new ModelInput()
            {
                Content = cleanMessage
            });
            return (MessageType)result.Prediction;
        }
    }
}
