namespace STAADModel
{
    // Units

    public enum StaadBaseUnitSystem
    {
        Imperial = 1,
        Metric = 2
    }

    public enum StaadForceInputUnit
    {
        KiloPound = 0,
        Pound = 1,
        KiloGram = 2,
        MetricTon = 3,
        Newton = 4,
        KiloNewton = 5,
        MegaNewton = 6,
        DecaNewton = 7
    }

    public enum StaadLengthInputUnit
    {
        Inch = 1,
        Feet = 2,
        CentiMeter = 3,
        Meter = 4,
        MilliMeter = 5,
        DeciMeter = 6,
        KiloMeter = 7
    }


    // Beams

    public enum BeamAxis
    {
        Longitudinal,
        Major,
        Minor
    }

    public enum BeamSpec
    {
        Unspecified,
        MemberTruss,
        TensionMember,
        CompressionMember,
        Cable,
        Joist
    }

    public enum BeamRelation
    {
        Other,
        Orthogonal,
        Parallel
    }

    public enum BeamRelativeDirection
    {
        OTHER,
        CODIRECTIONAL,
        CONTRADIRECTIONAL
    }

    public enum BeamType
    {
        UNKNOWN,
        COLUMN,
        POST,
        BEAM,
        BRACE
    }


    // Members

    public enum MemberType
    {
        OTHER,
        COLUMN,
        BEAM,
        BRACE,
        POST
    }

    public enum MemberRelation
    {
        OTHER,
        ORTHOGONAL,
        PARALLEL
    }

    public enum SupportType
    {
        UNSPECIFIED = 0,
        PINNED = 1,
        FIXED = 2,
        FIXEDBUT = 3
    }

    // Load cases

    public enum LoadCaseType
    {
        Dead = 0,
        Live = 1,
        RoofLive = 2,
        Wind = 3,
        Seismic = 4,
        Snow = 5,
        Fluids = 6,
        Soil = 7,
        Rain = 8,
        Ponding = 9,
        Dust = 10,
        Traffic = 11,
        Temp = 12,
        Imperfection = 13,
        Accidental = 14,
        Flood = 15,
        Ice = 16,
        WindIce = 17,
        CraneHook = 18,
        Mass = 19,
        Gravity = 20,
        Push = 21,
        None = 22
    }
}