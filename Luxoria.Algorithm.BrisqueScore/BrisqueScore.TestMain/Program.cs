// Define paths to the required model and range files
using System.Diagnostics;

const string modelPath = "models/brisque_model_live.yml";
const string rangePath = "models/brisque_range_live.yml";
const string imagePath = "img.jpg";

// Create an instance of BrisqueInterop
Debug.WriteLine("Creating an instance of BrisqueInterop...");
using (var brisque = new Luxoria.Algorithm.BrisqueScore.BrisqueInterop(modelPath, rangePath))
{
    // Compute BRISQUE score for an image
    double score = brisque.ComputeScore(imagePath);
    Debug.WriteLine($"BRISQUE Score for the image: {score}");
}
