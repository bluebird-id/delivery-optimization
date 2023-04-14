using FluentAssertions;
using RouteOptimization.Engine;
using RouteOptimization.Models;

namespace RouteOptimization.UnitTest
{
    public class EngineTest
    {
        RouteEngine _sut;

        public EngineTest()
        {
            _sut = new RouteEngine();
        }

        [Fact]
        public void Test1()
        {
            // arrange
            var input = new List<Models.ShipmentModel>
{
    new Models.ShipmentModel
    {
    Constraints= new Models.ConstraintsModel
            {
                Demands = new List<Models.DemandModel>
              {
              new Models.DemandModel{Type=0, Demands=0.0},
              new Models.DemandModel{Type=1, Demands=0.1},
              new Models.DemandModel{Type=2, Demands=0.2}
              },
            },
        },
    new Models.ShipmentModel
    {
    Constraints= new Models.ConstraintsModel
            {
                Demands = new List<Models.DemandModel>
              {
              new Models.DemandModel{Type=0, Demands=1.0},
              new Models.DemandModel{Type=1, Demands=1.1},
              new Models.DemandModel{Type=2, Demands=1.2}
              },
            },
        },
    };

            var expectation = new List<Models.ShipmentDemand>
    {
        new Models.ShipmentDemand{Type=0, Value=((EType)0).ToString(), Demands= new long[]{0L, 0L, 0L, 1L, 0L}},
        new Models.ShipmentDemand{Type=1, Value=((EType)1).ToString(), Demands=new long [] {0L, 0L, 0L, 1L, 0L}},
        new Models.ShipmentDemand{Type=2, Value=((EType)2).ToString(), Demands=new long []{0L, 0L, 0L, 1L, 0L}},
    };

            // act
            var result = _sut.GetShipmentDemands( input );

            // assert
            result.Should().BeEquivalentTo( expectation );
        }

        [Fact]
        public void GetPickUpPoints_Should_Returns_PickUpPoints()
        {
            // arrange
            var input = new List<Models.ShipmentModel>
{
    new Models.ShipmentModel
    {
    Constraints= new Models.ConstraintsModel
            {
                Demands = new List<Models.DemandModel>
              {
              new Models.DemandModel{Type=0, Demands=0.0},
              new Models.DemandModel{Type=1, Demands=0.1},
              new Models.DemandModel{Type=2, Demands=0.2}
              },
            },
        },
    new Models.ShipmentModel
    {
    Constraints= new Models.ConstraintsModel
            {
                Demands = new List<Models.DemandModel>
              {
              new Models.DemandModel{Type=0, Demands=1.0},
              new Models.DemandModel{Type=1, Demands=1.1},
              new Models.DemandModel{Type=2, Demands=1.2}
              },
            },
        },
    };

            var expectation = new int[ 2, 2 ] { { 1, 2 }, { 3, 4 } };

            // act
            var result = _sut.GetPickUpPoints( input );

            // assert
            for( int i = 0; i < result.Length; i++ )
            {
                for( int j = 0; j < result[ i ].Length; j++ )
                    Assert.Equal( result[ i ][ j ], expectation[ i, j ] );
            }
        }

    }
}