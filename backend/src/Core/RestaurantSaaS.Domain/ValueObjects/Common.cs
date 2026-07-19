using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string Line1 { get; }
    public string? Line2 { get; }
    public string City { get; }
    public string? State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public Address(string line1, string? line2, string city, string? state, string postalCode, string country)
    {
        Line1 = line1;
        Line2 = line2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Line1;
        yield return Line2;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() => $"{Line1}, {City}, {Country}";
}

public sealed class GeoCoordinates : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    public GeoCoordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}

public sealed class TimeRange : ValueObject
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public TimeRange(TimeOnly start, TimeOnly end)
    {
        Start = start;
        End = end;
    }

    public bool Contains(TimeOnly time) => Start <= End
        ? time >= Start && time <= End
        : time >= Start || time <= End; // overnight range, e.g. 18:00-02:00

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
