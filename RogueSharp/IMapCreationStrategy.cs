﻿using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp.Algorithms;
using RogueSharp.Random;

namespace RogueSharp
{
   /// <summary>
   /// An generic interface for creating a new IMap of a specified type
   /// </summary>
   /// <typeparam name="T">The type of IMap that will be created</typeparam>
   public interface IMapCreationStrategy<T> where T : IMap
   {
      /// <summary>
      /// Creates a new IMap of the specified type
      /// </summary>
      /// <returns>An IMap of the specified type</returns>
      T CreateMap();
   }
   /// <summary>
   /// The RandomRoomsMapCreationStrategy creates a Map of the specified type by placing rooms randomly and then connecting them with cooridors
   /// </summary>
   /// <typeparam name="T">The type of IMap that will be created</typeparam>
   public class RandomRoomsMapCreationStrategy<T> : IMapCreationStrategy<T> where T : IMap, new()
   {
      private readonly IRandom _random;
      private readonly int _height;
      private readonly int _maxRooms;
      private readonly int _roomMaxSize;
      private readonly int _roomMinSize;
      private readonly int _width;
      /// <summary>
      /// Constructs a new RandomRoomsMapCreationStrategy with the specified parameters
      /// </summary>
      /// <param name="width">The width of the Map to be created</param>
      /// <param name="height">The height of the Map to be created</param>
      /// <param name="maxRooms">The maximum number of rooms that will exist in the generated Map</param>
      /// <param name="roomMaxSize">The maximum width and height of each room that will be generated in the Map</param>
      /// <param name="roomMinSize">The minimum width and height of each room that will be generated in the Map</param>
      /// <param name="random">A class implementing IRandom that will be used to generate pseudo-random numbers necessary to create the Map</param>
      public RandomRoomsMapCreationStrategy( int width, int height, int maxRooms, int roomMaxSize, int roomMinSize, IRandom random )
      {
         _width = width;
         _height = height;
         _maxRooms = maxRooms;
         _roomMaxSize = roomMaxSize;
         _roomMinSize = roomMinSize;
         _random = random;
      }
      /// <summary>
      /// Constructs a new RandomRoomsMapCreationStrategy with the specified parameters
      /// </summary>
      /// <param name="width">The width of the Map to be created</param>
      /// <param name="height">The height of the Map to be created</param>
      /// <param name="maxRooms">The maximum number of rooms that will exist in the generated Map</param>
      /// <param name="roomMaxSize">The maximum width and height of each room that will be generated in the Map</param>
      /// <param name="roomMinSize">The minimum width and height of each room that will be generated in the Map</param>
      /// <remarks>Uses DotNetRandom as its RNG</remarks>
      public RandomRoomsMapCreationStrategy( int width, int height, int maxRooms, int roomMaxSize, int roomMinSize )
      {
         _width = width;
         _height = height;
         _maxRooms = maxRooms;
         _roomMaxSize = roomMaxSize;
         _roomMinSize = roomMinSize;
         _random = Singleton.DefaultRandom;
      }
      /// <summary>
      /// Creates a new IMap of the specified type.
      /// </summary>
      /// <remarks>
      /// The Map will be generated by trying to place rooms up to the maximum number specified in random locations around the Map.
      /// Each room will have a height and width between the minimum and maximum room size.
      /// If a room would be placed in such a way that it overlaps another room, it will not be placed.
      /// Once all rooms have have been placed, or thrown out because they overlap, cooridors will be created between rooms in a random manner.
      /// </remarks>
      /// <returns>An IMap of the specified type</returns>
      public T CreateMap()
      {
         var rooms = new List<Rectangle>();
         var map = new T();
         map.Initialize( _width, _height );

         for ( int r = 0; r < _maxRooms; r++ )
         {
            int roomWidth = _random.Next( _roomMinSize, _roomMaxSize );
            int roomHeight = _random.Next( _roomMinSize, _roomMaxSize );
            int roomXPosition = _random.Next( 0, _width - roomWidth - 1 );
            int roomYPosition = _random.Next( 0, _height - roomHeight - 1 );

            var newRoom = new Rectangle( roomXPosition, roomYPosition, roomWidth, roomHeight );
            bool newRoomIntersects = false;
            foreach ( Rectangle room in rooms )
            {
               if ( newRoom.Intersects( room ) )
               {
                  newRoomIntersects = true;
                  break;
               }
            }
            if ( !newRoomIntersects )
            {
               rooms.Add( newRoom );
            }
         }

         foreach ( Rectangle room in rooms )
         {
            MakeRoom( map, room );
         }

         for ( int r = 0; r < rooms.Count; r++ )
         {
            if ( r == 0 )
            {
               continue;
            }

            int previousRoomCenterX = rooms[r - 1].Center.X;
            int previousRoomCenterY = rooms[r - 1].Center.Y;
            int currentRoomCenterX = rooms[r].Center.X;
            int currentRoomCenterY = rooms[r].Center.Y;

            if ( _random.Next( 0, 2 ) == 0 )
            {
               MakeHorizontalTunnel( map, previousRoomCenterX, currentRoomCenterX, previousRoomCenterY );
               MakeVerticalTunnel( map, previousRoomCenterY, currentRoomCenterY, currentRoomCenterX );
            }
            else
            {
               MakeVerticalTunnel( map, previousRoomCenterY, currentRoomCenterY, previousRoomCenterX );
               MakeHorizontalTunnel( map, previousRoomCenterX, currentRoomCenterX, currentRoomCenterY );
            }
         }

         return map;
      }
      private static void MakeRoom( T map, Rectangle room )
      {
         for ( int x = room.Left + 1; x < room.Right; x++ )
         {
            for ( int y = room.Top + 1; y < room.Bottom; y++ )
            {
               map.SetCellProperties( x, y, true, true );
            }
         }
      }
      private static void MakeHorizontalTunnel( T map, int xStart, int xEnd, int yPosition )
      {
         for ( int x = Math.Min( xStart, xEnd ); x <= Math.Max( xStart, xEnd ); x++ )
         {
            map.SetCellProperties( x, yPosition, true, true );
         }
      }
      private static void MakeVerticalTunnel( T map, int yStart, int yEnd, int xPosition )
      {
         for ( int y = Math.Min( yStart, yEnd ); y <= Math.Max( yStart, yEnd ); y++ )
         {
            map.SetCellProperties( xPosition, y, true, true );
         }
      }
   }
   /// <summary>
   /// The BorderOnlyMapCreationStrategy creates a Map of the specified type by making an empty map with only the outermost border being solid walls
   /// </summary>
   /// <typeparam name="T">The type of IMap that will be created</typeparam>
   public class BorderOnlyMapCreationStrategy<T> : IMapCreationStrategy<T> where T : IMap, new()
   {
      private readonly int _height;
      private readonly int _width;
      /// <summary>
      /// Constructs a new BorderOnlyMapCreationStrategy with the specified parameters
      /// </summary>
      /// <param name="width">The width of the Map to be created</param>
      /// <param name="height">The height of the Map to be created</param>
      public BorderOnlyMapCreationStrategy( int width, int height )
      {
         _width = width;
         _height = height;
      }
      /// <summary>
      /// Creates a Map of the specified type by making an empty map with only the outermost border being solid walls
      /// </summary>
      /// <returns>An IMap of the specified type</returns>
      public T CreateMap()
      {
         var map = new T();
         map.Initialize( _width, _height );
         map.Clear( true, true );

         foreach ( Cell cell in map.GetCellsInRows( 0, _height - 1 ) )
         {
            map.SetCellProperties( cell.X, cell.Y, false, false );
         }

         foreach ( Cell cell in map.GetCellsInColumns( 0, _width - 1 ) )
         {
            map.SetCellProperties( cell.X, cell.Y, false, false );
         }

         return map;
      }
   }
   /// <summary>
   /// The StringDeserializeMapCreationStrategy creates a Map of the specified type from a string representation of the Map
   /// </summary>
   /// <typeparam name="T">The type of IMap that will be created</typeparam>
   public class StringDeserializeMapCreationStrategy<T> : IMapCreationStrategy<T> where T : IMap, new()
   {
      private readonly string _mapRepresentation;
      /// <summary>
      /// Constructs a new StringDeserializeMapCreationStrategy with the specified parameters
      /// </summary>
      /// <param name="mapRepresentation">A string representation of the Map to be created</param>
      public StringDeserializeMapCreationStrategy( string mapRepresentation )
      {
         _mapRepresentation = mapRepresentation;
      }
      /// <summary>
      /// Creates a Map of the specified type from a string representation of the Map
      /// </summary>
      /// <remarks>
      /// The following symbols represent Cells on the Map:
      /// - `.`: `Cell` is transparent and walkable
      /// - `s`: `Cell` is walkable (but not transparent)
      /// - `o`: `Cell` is transparent (but not walkable)
      /// - `#`: `Cell` is not transparent or walkable
      /// </remarks>
      /// <returns>An IMap of the specified type</returns>
      public T CreateMap()
      {
         string[] lines = _mapRepresentation.Replace( " ", "" ).Replace( "\r", "" ).Split( '\n' );

         int width = lines[0].Length;
         int height = lines.Length;
         var map = new T();
         map.Initialize( width, height );

         for ( int y = 0; y < height; y++ )
         {
            string line = lines[y];
            for ( int x = 0; x < width; x++ )
            {
               if ( line[x] == '.' )
               {
                  map.SetCellProperties( x, y, true, true );
               }
               else if ( line[x] == 's' )
               {
                  map.SetCellProperties( x, y, false, true );
               }
               else if ( line[x] == 'o' )
               {
                  map.SetCellProperties( x, y, true, false );
               }
               else if ( line[x] == '#' )
               {
                  map.SetCellProperties( x, y, false, false );
               }
            }
         }

         return map;
      }
   }
   /// <summary>
   /// The CaveMapCreationStrategy creates a Map of the specified type by using a cellular automata algorithm for creating a cave-like map.
   /// </summary>
   /// <seealso href="http://www.roguebasin.com/index.php?title=Cellular_Automata_Method_for_Generating_Random_Cave-Like_Levels">Cellular Automata Method from RogueBasin</seealso>
   /// <typeparam name="T">The type of IMap that will be created</typeparam>
   public class CaveMapCreationStrategy<T> : IMapCreationStrategy<T> where T : class, IMap, new()
   {
      private readonly int _width;
      private readonly int _height;
      private readonly int _fillProbability;
      private readonly int _totalIterations;
      private readonly int _cutoffOfBigAreaFill;
      private readonly IRandom _random;
      private T _map;
      /// <summary>
      /// Constructs a new CaveMapCreationStrategy with the specified parameters
      /// </summary>
      /// <param name="width">The width of the Map to be created</param>
      /// <param name="height">The height of the Map to be created</param>
      /// <param name="fillProbability">Recommend int between 40 and 60. Percent chance that a given cell will be a floor when randomizing all cells.</param>
      /// <param name="totalIterations">Recommend int between 2 and 5. Number of times to execute the cellular automata algorithm.</param>
      /// <param name="cutoffOfBigAreaFill">Recommend int less than 4. The interation number to switch from the large area fill algorithm to a nearest neighbor algorithm</param>
      /// <param name="random">A class implementing IRandom that will be used to generate pseudo-random numbers necessary to create the Map</param>
      public CaveMapCreationStrategy( int width, int height, int fillProbability, int totalIterations, int cutoffOfBigAreaFill, IRandom random )
      {
         _width = width;
         _height = height;
         _fillProbability = fillProbability;
         _totalIterations = totalIterations;
         _cutoffOfBigAreaFill = cutoffOfBigAreaFill;
         _random = random;
         _map = new T();
      }
      /// <summary>
      /// Constructs a new CaveMapCreationStrategy with the specified parameters
      /// </summary>
      /// <param name="width">The width of the Map to be created</param>
      /// <param name="height">The height of the Map to be created</param>
      /// <param name="fillProbability">Recommend int between 40 and 60. Percent chance that a given cell will be a floor when randomizing all cells.</param>
      /// <param name="totalIterations">Recommend int between 2 and 5. Number of times to execute the cellular automata algorithm.</param>
      /// <param name="cutoffOfBigAreaFill">Recommend int less than 4. The interation number to switch from the large area fill algorithm to a nearest neighbor algorithm</param>
      /// <remarks>Uses DotNetRandom as its RNG</remarks>
      public CaveMapCreationStrategy( int width, int height, int fillProbability, int totalIterations, int cutoffOfBigAreaFill )
      {
         _width = width;
         _height = height;
         _fillProbability = fillProbability;
         _totalIterations = totalIterations;
         _cutoffOfBigAreaFill = cutoffOfBigAreaFill;
         _random = Singleton.DefaultRandom;
         _map = new T();
      }
      /// <summary>
      /// Creates a new IMap of the specified type.
      /// </summary>
      /// <remarks>
      /// The map will be generated using cellular automata. First each cell in the map will be set to a floor or wall randomly based on the specified fillProbability.
      /// Next each cell will be examined a number of times, and in each iteration it may be turned into a wall if there are enough other walls near it.
      /// Once finished iterating and examining neighboring cells, any isolated map regions will be connected with paths.
      /// </remarks>
      /// <returns>An IMap of the specified type</returns>
      public T CreateMap()
      {
         _map.Initialize( _width, _height );

         RandomlyFillCells();

         for ( int i = 0; i < _totalIterations; i++ )
         {
            if ( i < _cutoffOfBigAreaFill )
            {
               CellularAutomataBigAreaAlgorithm();
            }
            else if ( i >= _cutoffOfBigAreaFill )
            {
               CellularAutomaNearestNeighborsAlgorithm();
            }
         }

         ConnectCaves();

         return _map;
      }
      private void RandomlyFillCells()
      {
         foreach ( Cell cell in _map.GetAllCells() )
         {
            if ( IsBorderCell( cell ) )
            {
               _map.SetCellProperties( cell.X, cell.Y, false, false );
            }
            else if ( _random.Next( 1, 100 ) < _fillProbability )
            {
               _map.SetCellProperties( cell.X, cell.Y, true, true );
            }
            else
            {
               _map.SetCellProperties( cell.X, cell.Y, false, false );
            }
         }
      }
      private void CellularAutomataBigAreaAlgorithm()
      {
         var updatedMap = _map.Clone() as T;

         foreach ( Cell cell in _map.GetAllCells() )
         {
            if ( IsBorderCell( cell ) )
            {
               continue;
            }
            if ( ( CountWallsNear( cell, 1 ) >= 5 ) || ( CountWallsNear( cell, 2 ) <= 2 ) )
            {
               updatedMap.SetCellProperties( cell.X, cell.Y, false, false );
            }
            else
            {
               updatedMap.SetCellProperties( cell.X, cell.Y, true, true );
            }
         }

         _map = updatedMap;
      }
      private void CellularAutomaNearestNeighborsAlgorithm()
      {
         var updatedMap = _map.Clone() as T;

         foreach ( Cell cell in _map.GetAllCells() )
         {
            if ( IsBorderCell( cell ) )
            {
               continue;
            }
            if ( CountWallsNear( cell, 1 ) >= 5 )
            {
               updatedMap.SetCellProperties( cell.X, cell.Y, false, false );
            }
            else
            {
               updatedMap.SetCellProperties( cell.X, cell.Y, true, true );
            }
         }

         _map = updatedMap;
      }
      private bool IsBorderCell( Cell cell )
      {
         return cell.X == 0 || cell.X == _map.Width - 1
                || cell.Y == 0 || cell.Y == _map.Height - 1;
      }
      private int CountWallsNear( Cell cell, int distance )
      {
         int count = 0;
         foreach ( Cell nearbyCell in _map.GetCellsInArea( cell.X, cell.Y, distance ) )
         {
            if ( nearbyCell.X == cell.X && nearbyCell.Y == cell.Y )
            {
               continue;
            }
            if ( !nearbyCell.IsWalkable )
            {
               count++;
            }
         }
         return count;
      }
      private void ConnectCaves()
      {
         var mapAnalyzer = new MapAnalyzer( _map );
         List<MapSection> mapSections = mapAnalyzer.GetMapSections();
         var unionFind = new UnionFind( mapSections.Count );
         while ( unionFind.Count > 1 )
         {
            for ( int i = 0; i < mapSections.Count; i++ )
            {
               int closestMapSectionIndex = FindNearestMapSection( mapSections, i, unionFind );
               MapSection closestMapSection = mapSections[closestMapSectionIndex];
               IEnumerable<Cell> tunnelCells = _map.GetCellsAlongLine( mapSections[i].Bounds.Center.X, mapSections[i].Bounds.Center.Y,
                  closestMapSection.Bounds.Center.X, closestMapSection.Bounds.Center.Y );
               Cell previousCell = null;
               foreach ( Cell cell in tunnelCells )
               {
                  _map.SetCellProperties( cell.X, cell.Y, true, true );
                  if ( previousCell != null )
                  {
                     if ( cell.X != previousCell.X || cell.Y != previousCell.Y )
                     {
                        _map.SetCellProperties( cell.X + 1, cell.Y, true, true );
                     }
                  }
                  previousCell = cell;
               }
               unionFind.Union( i, closestMapSectionIndex );
            }
         }
      }
      private static int FindNearestMapSection( IList<MapSection> mapSections, int mapSectionIndex, UnionFind unionFind )
      {
         MapSection start = mapSections[mapSectionIndex];
         int closestIndex = mapSectionIndex;
         int distance = Int32.MaxValue;
         for ( int i = 0; i < mapSections.Count; i++ )
         {
            if ( i == mapSectionIndex )
            {
               continue;
            }
            if ( unionFind.Connected( i, mapSectionIndex ) )
            {
               continue;
            }
            int distanceBetween = DistanceBetween( start, mapSections[i] );
            if ( distanceBetween < distance )
            {
               distance = distanceBetween;
               closestIndex = i;
            }
         }
         return closestIndex;
      }
      private static int DistanceBetween( MapSection startMapSection, MapSection destinationMapSection )
      {
         return Math.Abs( startMapSection.Bounds.Center.X - destinationMapSection.Bounds.Center.X ) + Math.Abs( startMapSection.Bounds.Center.Y - destinationMapSection.Bounds.Center.Y );
      }

      private class MapAnalyzer
      {
         private readonly IMap _map;
         private readonly List<MapSection> _mapSections;
         private readonly PathFinder _pathFinder;
         public MapAnalyzer( IMap map )
         {
            _map = map;
            _mapSections = new List<MapSection>();
            _pathFinder = new PathFinder( _map );
         }
         public List<MapSection> GetMapSections()
         {
            foreach ( Cell cell in _map.GetAllCells() )
            {
               if ( !cell.IsWalkable )
               {
                  continue;
               }
               bool foundSection = false;
               foreach ( MapSection mapSection in _mapSections )
               {
                  var shortestPath = _pathFinder.ShortestPath( cell, mapSection.Cells.First() );

                  if ( shortestPath.Start != null )
                  {
                     mapSection.AddCell( cell );
                     foundSection = true;
                     break;
                  }
               }
               if ( !foundSection )
               {
                  var mapSection = new MapSection();
                  mapSection.AddCell( cell );
                  _mapSections.Add( mapSection );
               }
            }
            return _mapSections;
         }
      }
      private class MapSection
      {
         private int _top;
         private int _bottom;
         private int _right;
         private int _left;
         public Rectangle Bounds
         {
            get
            {
               return new Rectangle( _left, _top, _right - _left + 1, _bottom - _top + 1 );
            }
         }
         public HashSet<Cell> Cells { get; private set; }
         public MapSection()
         {
            Cells = new HashSet<Cell>();
            _top = int.MaxValue;
            _left = int.MaxValue;
         }
         public void AddCell( Cell cell )
         {
            Cells.Add( cell );
            UpdateBounds();
         }
         private void UpdateBounds()
         {
            foreach ( Cell cell in Cells )
            {
               if ( cell.X > _right )
               {
                  _right = cell.X;
               }
               if ( cell.X < _left )
               {
                  _left = cell.X;
               }
               if ( cell.Y > _bottom )
               {
                  _bottom = cell.Y;
               }
               if ( cell.Y < _top )
               {
                  _top = cell.Y;
               }
            }
         }
         public override string ToString()
         {
            return string.Format( "Bounds: {0}", Bounds );
         }
      }
   }
}
