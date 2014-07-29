using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;
using TriangulationTopology.Properties;
using Utils;
using PointsFileReader;


namespace TriangulationTopology
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Logger logger = Logger.GetInstance();
            
            string firstPtsFilePath = Settings.Default.Properties["firstPtsFile"].DefaultValue.ToString();
            string secondPtsFilePath = Settings.Default.Properties["secondPtsFile"].DefaultValue.ToString();

            logger.Debug("Gonna load the points from the files");

            Point[] points1 = SpaceDelimitedPointsFileReader.GetInstance().Read(firstPtsFilePath);
            Point[] points2 = SpaceDelimitedPointsFileReader.GetInstance().Read(secondPtsFilePath);
            
            logger.Debug("Points loaded");
            
            //TODO: if the creation is too expensive, consider being an arab and use the CreateTriangles code
            //TODO: explicitly to create both triangle arrays in the same loop..
            Triangle[] triangles1 = AlgorithmCalculations.CreateTriangles(points1);
            Triangle[] triangles2 = AlgorithmCalculations.CreateTriangles(points2);

            logger.Debug("All triangles created");

            //check all pairs. get the qualified pairs only. dict from avg. orientation diff to tuple of the triangles
            //TODO: check if capping the dict to triangles1Xtriangles2 size will outperform
            Dictionary<double, Tuple<Triangle, Triangle>> filteredTriPairs =
                AlgorithmCalculations.FilterAndCalculateOrientationDiff(triangles1, triangles2);

            logger.Debug("All triangles filtered");
            //get the most probable orientation
            double probableOrientationFix = AlgorithmCalculations.CalculateMostProbableOrientation(filteredTriPairs);

            logger.Debug(string.Format("Most probable orientation calculated: {0}", probableOrientationFix));

            watch.Stop();

            Console.WriteLine("Time elapsed: {0}", watch.ElapsedMilliseconds);
            Console.ReadKey();




        }

    }
}
