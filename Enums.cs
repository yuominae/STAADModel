namespace STAADModel
{
    public enum STAADBASEUNITSYSTEM
    {
        IMPERIAL = 1,
        METRIC = 2
    }

    public enum STAADFORCEINPUTUNIT
    {
        KILOPOUND = 0,
        POUND = 1,
        KILOGRAM = 2,
        METRICTON = 3,
        NEWTON = 4,
        KILONEWTON = 5,
        MEGANEWTON = 6,
        DECANEWTON = 7
    }

    public enum STAADLENGTHINPUTUNIT
    {
        INCH = 1,
        FEET = 2,
        CENTIMETER = 3,
        METER = 4,
        MILLIMETER = 5,
        DECIMETER = 6,
        KILOMETER = 7
    }

    #region Beam enums

    public enum BEAMAXIS
    {
        LONGITUDINAL,
        MAJOR,
        MINOR
    }

    public enum BEAMSPEC
    {
        UNSPECIFIED,
        MEMBERTRUSS,
        TENSIONMEMBER,
        COMPRESSIONMEMBER,
        CABLE,
        JOIST
    }

    public enum BEAMRELATION
    {
        OTHER,
        ORTHOGONAL,
        PARALLEL
    }

    public enum BEAMRELATIVEDIRECTION
    {
        OTHER,
        CODIRECTIONAL,
        CONTRADIRECTIONAL
    }

    public enum BEAMTYPE
    {
        UNKNOWN,
        COLUMN,
        POST,
        BEAM,
        BRACE
    }

    #endregion Beam enums

    #region Member enums

    public enum MEMBERTYPE
    {
        OTHER,
        COLUMN,
        BEAM,
        BRACE,
        POST
    }

    public enum MEMBERRELATION
    {
        OTHER,
        ORTHOGONAL,
        PARALLEL
    }

    #endregion Member enums

    #region Suppport enums

    /// <summary>
    /// Staad support type description. Numeric values correspond to OpenSTAADUI Support.getSupportType() return value
    /// </summary>
    public enum SUPPORTTYPE
    {
        UNSPECIFIED = 0,
        PINNED = 1,
        FIXED = 2,
        FIXEDBUT = 3
    }

    #endregion Suppport enums

    #region Load case enums

    public enum LOADCASETYPE
    {
        DEAD = 0,
        LIVE = 1,
        ROOFLIVE = 2,
        WIND = 3,
        SEISMIC = 4,
        SNOW = 5,
        FLUIDS = 6,
        SOIL = 7,
        RAIN = 8,
        PONDING = 9,
        DUST = 10,
        TRAFFIC = 11,
        TEMP = 12,
        IMPERFECTION = 13,
        ACCIDENTAL = 14,
        FLOOD = 15,
        ICE = 16,
        WINDICE = 17,
        CRANEHOOK = 18,
        MASS = 19,
        GRAVITY = 20,
        PUSH = 21,
        NONE = 22
    }

    #endregion Load case enums
}