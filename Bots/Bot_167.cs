namespace auto_Bot_167;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Betelchess: moves pieces using the current position of Betelgeuse in the sky as seen from the Mount Wilson Observatory
public class Bot_167 : IChessBot
{
    // Sky coordinates of Betelgeuse
    // Source: SIMBAD
    static double RA = 88.79293899;
    static double dec = 7.407064;

    // Using a 6-meter interferometer mounted on the front of the 2.5-meter telescope located at Mount Wilson Observatory,
    // the angular diameter of Betelgeuse was measured for the first time in 1920 by astronomers Albert A. Michelson and Francis G. Pease
    static double lat = 34.22503;
    static double lon = -118.05719;
    public Move Think(Board board, Timer timer)
    {
        bool botIsWhite = board.IsWhiteToMove;

        DateTime date = DateTime.UtcNow;

        // alt: Altitude of Betelgeuse in degrees
        // az: Azimuth of Betelgeuse in degrees

        (double alt, double az) = CalcAzAlt(date);
        if (botIsWhite)
        {
            az = (az + 180.0) % 360.0;
        }

        // Get all of the positions of the bot's pieces

        PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King };

        List<int> x = new List<int>();
        List<int> y = new List<int>();

        foreach (PieceType pieceType in pieceTypes)
        {
            PieceList pieceList = board.GetPieceList(pieceType, botIsWhite);
            foreach (Piece piece in pieceList)
            {
                Square square = piece.Square;
                x.Add(square.File);
                y.Add(square.Rank);
            }
        }

        // Get the closest distance to a line starting at cx, cy extended in the direction of az

        double cx = x.Average();
        double cy = y.Average();

        double azRad = DegreesToRadians(az);
        double azx = cx + Math.Sin(azRad);
        double azy = cy + Math.Cos(azRad);

        List<int> newx = new List<int>();
        List<int> newy = new List<int>();

        List<double> distances = new List<double>();

        for (int i = 0; i < x.Count; i++)
        {
            double dotProduct = (x[i] - cx) * (azx - cx) + (y[i] - cy) * (azy - cy);
            if (dotProduct < 0.0)
            {
                continue;
            }

            newx.Add(x[i]);
            newy.Add(y[i]);

            double distance = Math.Abs((azx - cx) * (cy - y[i]) - (cx - x[i]) * (azy - cy)) / Math.Sqrt((azx - cx) * (azx - cx) + (azy - cy) * (azy - cy));
            distances.Add(distance);
        }

        // Get the moves of the closest piece to the line, if there are no legal moves jump to the next closest piece, and so on

        var sorted = distances.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderBy(x => x.Key).ToList();
        List<int> indices = sorted.Select(x => x.Value).ToList();

        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = allMoves[0];

        foreach (int index in indices)
        {
            int selectedFile = newx[index];
            int selectedRank = newy[index];

            List<Move> moves = new List<Move>();

            foreach (Move move in allMoves)
            {
                Square square = move.StartSquare;
                if (square.File == selectedFile && square.Rank == selectedRank)
                {
                    moves.Add(move);
                }
            }

            if (moves.Count == 0)
            {
                continue;
            }

            // Calculate the distance the piece moves for each move

            List<double> moveDistances = new List<double>();

            foreach (Move move in moves)
            {
                Square targetSquare = move.TargetSquare;

                int targetFile = targetSquare.File;
                int targetRank = targetSquare.Rank;

                double moveDistance = Math.Sqrt((targetFile - selectedFile) * (targetFile - selectedFile) + (targetRank - selectedRank) * (targetRank - selectedRank));

                double dotProduct = (targetFile - cx) * (azx - cx) + (targetRank - cy) * (azy - cy);
                if (dotProduct < 0.0)
                {
                    moveDistance = -moveDistance;
                }

                moveDistances.Add(moveDistance);
            }

            // Select a move such that alt = -90 corresponds to the smallest available distance and alt = 90 the largest

            double minMoveDistance = moveDistances.Min();
            double maxMoveDistance = moveDistances.Max();

            double targetDistance = minMoveDistance + (alt + 90) * (maxMoveDistance - minMoveDistance) / 180;

            double minDifference = Double.MaxValue;
            for (int i = 0; i < moves.Count; i++)
            {
                double difference = Math.Abs(moveDistances[i] - targetDistance);
                if (difference < minDifference)
                {
                    moveToPlay = moves[i];
                    minDifference = difference;
                }
            }

            break;
        }
        return moveToPlay;
    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
    public static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    public static (double, double) CalcAzAlt(DateTime date)
    {
        double UT = date.Hour + date.Minute / 60.0 + date.Second / 3600.0;
        double JD = date.ToOADate() + 2415018.5;
        double LST = (100.4606184 + 0.9856473662862 * (JD - 2451545.0) + lon + 15.0 * UT) % 360.0;
        double HA = DegreesToRadians(LST - RA);

        double decRad = DegreesToRadians(dec);
        double latRad = DegreesToRadians(lat);

        double alt = RadiansToDegrees(Math.Asin(Math.Sin(latRad) * Math.Sin(decRad) + Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(HA)));
        double az = RadiansToDegrees(-Math.Atan2(Math.Cos(decRad) * Math.Sin(HA), -Math.Sin(latRad) * Math.Cos(decRad) * Math.Cos(HA) + Math.Cos(latRad) * Math.Sin(decRad))) % 360.0;
        return (alt, az);
    }
}