using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    //===================Availabilty Enum Values============================

    public enum Weekday
    {
        [Description("Morning Slot: 6-10AM")]
        MorningSlot = 1,
        [Description("Evening Slot: 8PM-12AM")]
        EveningSlot = 2,
        [Description("Late Night Slot: 12AM-6AM")]
        LateNightSlot = 3,
    }
    public enum Weekend
    {
        [Description("Morning Slot: 6-10AM")]
        MorningSlot = 1,
        [Description("Day Slot: 10AM-8PM")]
        DaySlot = 2,
        [Description("Evening Slot: 8PM-12AM")]
        EveningSlot = 3,
        [Description("Late Night Slot: 12AM-6AM")]
        LateNightSlot = 4,
    }
    //===================Project Enum Values============================

    public enum NatureOfProject
    {
        [Description("Development From Scratch")]
        DevelopmentFromScratch = 1,
        [Description("Enhancement")]
        Enhancement = 2,
        [Description("Support")]
        Support = 3,
        [Description("Maintenance")]
        Maintenance = 4,
    }
    //===================Personal Enum Values============================

    public enum Degree
    {
        [Description("Engineerning")]
        Engineerning = 1,
        [Description("Diploma")]
        Diploma = 2,
        [Description("Degree- Science")]
        DegreeScience = 3,
        [Description("Degree- Non Science")]
        DegreeNonScience = 4,
    }
    public enum EducationMode
    {
        [Description("Full Time Residential")]
        FullTimeResidential = 1,
        [Description("Part Time Online")]
        PartTimeOnline = 2,
        [Description("Part Time Distance Eduation")]
        PartTimeDistanceEduation = 3,
    }
    public enum GraduationType
    {
        [Description("Graduation")]
        Graduation = 1,
        [Description("Post Graduation")]
        PostGraduation = 2
    }

    public enum CollegeType
    {
        [Description("Tier1")]
        TierOne = 1,
        [Description("Tier2")]
        TierTwo = 2,
        [Description("Tier3")]
        TierThree = 3,
        [Description("Tier4")]
        TierFour = 4,
        [Description("Tier5")]
        TierFive = 5
    }
    //===================Personal Enum Values============================

    public enum Gender
    {
        [Description("Male")]
        Male = 1,
        [Description("Female")]
        Female = 2
    }
    public enum WorkCountry
    {
        [Description("India")]
        India = 1,
        [Description("US")]
        OutOfIndia = 2,
        [Description("Both India and US")]
        BothIndiaandOutOfIndia = 3
    }
    public enum IndiaPrice
    {
        [Description("600")]
        Sixhundred = 600,
        [Description("800")]
        Eigthhundred = 800,
        [Description("1000")]
        Onethousand = 1000,
        [Description("1200")]
        Twelvehundred = 1200,
        [Description("1500")]
        Fifteenhundred = 1500,
        [Description("2000")]
        Twothousand = 2000
    }
    public enum OutSideIndiaPrice
    {
        [Description("900")]
        Ninehundred = 900,
        [Description("1200")]
        Twelvehundred = 1200,
        [Description("1500")]
        Fifteenhundred = 1500,
        [Description("1800")]
        Eighteenhundred = 1800,
        [Description("2500")]
        Twentyfivehundred = 2500,
        [Description("3000")]
        Threethousand = 3000
    }
    public enum Country
    {
        [Description("Africa")]
        Africa = 1,
        [Description("Algeria")]
        Algeria = 2,
        [Description("Cameroon")]
        Cameroon = 3,
        [Description("Egypt")]
        Egypt = 4,
        [Description("Ghana")]
        Ghana = 5,
        [Description("Kenya")]
        Kenya = 6,
        [Description("Morocco")]
        Morocco = 7,
        [Description("Nigeria")]
        Nigeria = 8,
        [Description("South Africa")]
        SouthAfrica = 9,
        [Description("Cape Town Area, South Africa")]
        CapeTownAreaSouthAfrica = 10,
        [Description("Durban Area, South Africa")]
        DurbanAreaSouthAfrica = 11,
        [Description("Johannesburg Area, South Africa")]
        JohannesburgAreaSouthAfrica = 12,
        [Description("Tanzania")]
        Tanzania = 13,
        [Description("Tunisia")]
        Tunisia = 14,
        [Description("Uganda")]
        Uganda = 15,
        [Description("Zimbabwe")]
        Zimbabwe = 16,
        [Description("Antarctica")]
        Antarctica = 17,
        [Description("Asia")]
        Asia = 18,
        [Description("Bangladesh")]
        Bangladesh = 19,
        [Description("China")]
        China = 20,
        [Description("Beijing City, China")]
        BeijingCityChina = 21,
        [Description("Guangzhou, Guangdong, China")]
        GuangzhouGuangdongChina = 22,
        [Description("Shanghai City, China")]
        ShanghaiCityChina = 23,
        [Description("Shenzhen, Guangdong, China")]
        ShenzhenGuangdongChina = 24,
        [Description("Hong Kong")]
        HongKong = 25,
        [Description("India")]
        India = 26,
        [Description("Andaman & Nicobar Islands")]
        AndamanAndNicobarIslands = 27,
        [Description("Andhra Pradesh")]
        AndhraPradesh = 28,
        [Description("Hyderabad Area, India")]
        HyderabadAreaIndia = 29,
        [Description("Arunachal Pradesh")]
        ArunachalPradesh = 30,
        [Description("Assam")]
        Assam = 31,
        [Description("Bihar")]
        Bihar = 32,
        [Description("Chandigarh")]
        Chandigarh = 33,
        [Description("Chattisgarh")]
        Chattisgarh = 34,
        [Description("Dadra& Nagar Haveli")]
        DadraAndNagarHaveli = 35,
        [Description("Daman & Diu")]
        DamanAndDiu = 36,
        [Description("Delhi")]
        Delhi = 37,
        [Description("Goa")]
        Goa = 38,
        [Description("Gujarat")]
        Gujarat = 39,
        [Description("Ahmedabad Area, India")]
        AhmedabadAreaIndia = 40,
        [Description("Vadodara Area, India")]
        VadodaraAreaIndia = 41,
        [Description("Haryana")]
        Haryana = 42,
        [Description("Gurgaon, India")]
        GurgaonIndia = 43,
        [Description("New Delhi Area, India")]
        NewDelhiAreaIndia = 44,
        [Description("Himachal Pradesh")]
        HimachalPradesh = 45,
        [Description("Jammu & Kashmir")]
        JammuAndKashmir = 46,
        [Description("Jharkhand")]
        Jharkhand = 47,
        [Description("Karnataka")]
        Karnataka = 48,
        [Description("Bengaluru Area, India")]
        BengaluruAreaIndia = 49,
        [Description("Kerala")]
        Kerala = 50,
        [Description("Cochin Area, India")]
        CochinAreaIndia = 51,
        [Description("Algeria")]
        Lakshadweep = 52,
        [Description("Madhya Pradesh")]
        MadhyaPradesh = 53,
        [Description("Indore Area, India")]
        IndoreAreaIndia = 54,
        [Description("Maharashtra")]
        Maharashtra = 55,
        [Description("Mumbai Area, India")]
        MumbaiAreaIndia = 56,
        [Description("Nagpur Area, India")]
        NagpurAreaIndia = 57,
        [Description("Pune Area, India")]
        PuneAreaIndia = 58,
        [Description("Manipur")]
        Manipur = 59,
        [Description("Meghalaya")]
        Meghalaya = 60,
        [Description("Mizoram")]
        Mizoram = 61,
        [Description("Nagaland")]
        Nagaland = 62,
        [Description("Orissa")]
        Orissa = 63,
        [Description("Pondicherry")]
        Pondicherry = 64,
        [Description("Punjab")]
        Punjab = 65,
        [Description("Chandigarh Area, India")]
        ChandigarhAreaIndia = 66,
        [Description("Jaipur Area, India")]
        JaipurAreaIndia = 67,
        [Description("Sikkim")]
        Sikkim = 68,
        [Description("Tamil Nadu")]
        TamilNadu = 69,
        [Description("Chennai Area, India")]
        ChennaiAreaIndia = 70,
        [Description("Coimbatore Area, India")]
        CoimbatoreAreaIndia = 71,
        [Description("Tripura")]
        Tripura = 72,
        [Description("Uttar Pradesh")]
        UttarPradesh = 73,
        [Description("Lucknow Area, India")]
        LucknowAreaIndia = 74,
        [Description("Noida Area, India")]
        NoidaAreaIndia = 78,
        [Description("Uttarakhand")]
        Uttarakhand = 76,
        [Description("West Bengal")]
        WestBengal = 77,
        [Description("Kolkata Area, India")]
        KolkataAreaIndia = 78,
        [Description("Indonesia")]
        Indonesia = 79,
        [Description("Greater Jakarta Area, Indonesia")]
        GreaterJakartaAreaIndonesia = 80,
        [Description("Japan")]
        Japan = 81,
        [Description("Korea")]
        Korea = 82,
        [Description("Gangnam-gu, Seoul, Korea")]
        GangnamguSeoulKorea = 83,
        [Description("Malaysia")]
        Malaysia = 84,
        [Description("Kuala Lumpur, Malaysia")]
        KualaLumpurMalaysia = 85,
        [Description("Selangor, Malaysia")]
        SelangorMalaysia = 86,
        [Description("Nepal")]
        Nepal = 87,
        [Description("Philippines")]
        Philippines = 88,
        [Description("Singapore")]
        Singapore = 89,
        [Description("Sri Lanka")]
        SriLanka = 90,
        [Description("Taiwan")]
        Taiwan = 91,
        [Description("Thailand")]
        Thailand = 92,
        [Description("Vietnam")]
        Vietnam = 93,
        [Description("Europe")]
        Europe = 94,
        [Description("Austria")]
        Austria = 95,
        [Description("Belgium")]
        Belgium = 96,
        [Description("Antwerp Area, Belgium")]
        AntwerpAreaBelgium = 97,
        [Description("Brussels Area, Belgium")]
        BrusselsAreaBelgium = 98,
        [Description("Bulgaria")]
        Bulgaria = 100,
        [Description("Croatia")]
        Croatia = 101,
        [Description("Czech Republic")]
        CzechRepublic = 102,
        [Description("Denmark")]
        Denmark = 103,
        [Description("Copenhagen Area, Denmark")]
        CopenhagenAreaDenmark = 104,
        [Description("Odense Area, Denmark")]
        OdenseAreaDenmark = 105,
        [Description("Ålborg Area, Denmark")]
        ÅlborgAreaDenmark = 106,
        [Description("Århus Area, Denmark")]
        ÅrhusAreaDenmark = 107,
        [Description("Finland")]
        Finland = 108,
        [Description("France")]
        France = 109,
        [Description("Lille Area, France")]
        LilleAreaFrance = 110,
        [Description("Lyon Area, France")]
        LyonAreaFrance = 111,
        [Description("Marseille Area, France")]
        MarseilleAreaFrance = 112,
        [Description("Nice Area, France")]
        NiceAreaFrance = 113,
        [Description("Paris Area, France")]
        ParisAreaFrance = 114,
        [Description("Toulouse Area, France")]
        ToulouseAreaFrance = 115,
        [Description("Germany")]
        Germany = 116,
        [Description("Cologne Area, Germany")]
        CologneAreaGermany = 117,
        [Description("Frankfurt Am Main Area, Germany")]
        FrankfurtAmMainAreaGermany = 118,
        [Description("Munich Area, Germany")]
        MunichAreaGermany = 119,
        [Description("Greece")]
        Greece = 120,
        [Description("Hungary")]
        Hungary = 121,
        [Description("Ireland")]
        Ireland = 122,
        [Description("Bologna Area, Italy")]
        BolognaAreaItaly = 123,
        [Description("Milan Area, Italy")]
        MilanAreaItaly = 124,
        [Description("Rome Area, Italy")]
        RomeAreaItaly = 125,
        [Description("Turin Area, Italy")]
        TurinAreaItaly = 126,
        [Description("Venice Area, Italy")]
        VeniceAreaItaly = 127,
        [Description("Lithuania")]
        Lithuania = 128,
        [Description("Netherlands")]
        Netherlands = 129,
        [Description("Almere Stad Area, Netherlands")]
        AlmereStadAreaNetherlands = 130,
        [Description("Amsterdam Area, Netherlands")]
        AmsterdamAreaNetherlands = 131,
        [Description("Apeldoorn Area, Netherlands")]
        ApeldoornAreaNetherlands = 132,
        [Description("Breda Area, Netherlands")]
        BredaAreaNetherlands = 133,
        [Description("Eindhoven Area, Netherlands")]
        EindhovenAreaNetherlands = 134,
        [Description("Enschede Area, Netherlands")]
        EnschedeAreaNetherlands = 135,
        [Description("Groningen Area, Netherlands")]
        GroningenAreaNetherlands = 136,
        [Description("Nijmegen Area, Netherlands")]
        NijmegenAreaNetherlands = 137,
        [Description("Rotterdam Area, Netherlands")]
        RotterdamAreaNetherlands = 138,
        [Description("The Hague Area, Netherlands")]
        TheHagueAreaNetherlands = 139,
        [Description("Tilburg Area, Netherlands")]
        TilburgAreaNetherlands = 140,
        [Description("Utrecht Area, Netherlands")]
        UtrechtAreaNetherlands = 141,
        [Description("Norway")]
        Norway = 142,
        [Description("Oslo Area, Norway")]
        OsloAreaNorway = 143,
        [Description("Portugal")]
        Portugal = 144,
        [Description("Lisbon Area, Portugal")]
        LisbonAreaPortugal = 145,
        [Description("Porto Area, Portugal")]
        PortoAreaPortugal = 146,
        [Description("Romania")]
        Romania = 147,
        [Description("Russian Federation")]
        RussianFederation = 148,
        [Description("Serbia")]
        Serbia = 149,
        [Description("Slovak Republic")]
        SlovakRepublic = 150,
        [Description("Spain")]
        Spain = 151,
        [Description("Barcelona Area, Spain")]
        BarcelonaAreaSpain = 152,
        [Description("Madrid Area, Spain")]
        MadridAreaSpain = 153,
        [Description("Sweden")]
        Sweden = 154,
        [Description("Switzerland")]
        Switzerland = 155,
        [Description("Geneva Area, Switzerland")]
        GenevaAreaSwitzerland = 156,
        [Description("Zürich Area, Switzerland")]
        ZürichAreaSwitzerland = 157,
        [Description("Turkey")]
        Turkey = 158,
        [Description("Istanbul, Turkey")]
        IstanbulTurkey = 159,
        [Description("Ukraine")]
        Ukraine = 160,
        [Description("United Kingdom")]
        UnitedKingdom = 161,
        [Description("Birmingham, United Kingdom")]
        BirminghamUnitedKingdom = 162,
        [Description("Brighton, United Kingdom")]
        BrightonUnitedKingdom = 163,
        [Description("Bristol, United Kingdom")]
        BristolUnitedKingdom = 164,
        [Description("Cambridge, United Kingdom")]
        CambridgeUnitedKingdom = 165,
        [Description("Chelmsford, United Kingdom")]
        ChelmsfordUnitedKingdom = 166,
        [Description("Coventry, United Kingdom")]
        CoventryUnitedKingdom = 167,
        [Description("Edinburgh, United Kingdom")]
        EdinburghUnitedKingdom = 168,
        [Description("Glasgow, United Kingdom")]
        GlasgowUnitedKingdom = 169,
        [Description("Gloucester, United Kingdom")]
        GloucesterUnitedKingdom = 170,
        [Description("Guildford, United Kingdom")]
        GuildfordUnitedKingdom = 171,
        [Description("Harrow, United Kingdom")]
        HarrowUnitedKingdom = 172,
        [Description("Hemel Hempstead, United Kingdom")]
        HemelHempsteadUnitedKingdom = 173,
        [Description("Kingston upon Thames, United Kingdom")]
        KingstonuponThamesUnitedKingdom = 174,
        [Description("Leeds, United Kingdom")]
        LeedsUnitedKingdom = 175,
        [Description("Leicester, United Kingdom")]
        LeicesterUnitedKingdom = 176,
        [Description("London, United Kingdom")]
        LondonUnitedKingdom = 177,
        [Description("Manchester, United Kingdom")]
        ManchesterUnitedKingdom = 178,
        [Description("Milton Keynes, United Kingdom")]
        MiltonKeynesUnitedKingdom = 179,
        [Description("Newcastle upon Tyne, United Kingdom")]
        NewcastleuponTyneUnitedKingdom = 180,
        [Description("Northampton, United Kingdom")]
        NorthamptonUnitedKingdom = 181,
        [Description("Nottingham, United Kingdom")]
        NottinghamUnitedKingdom = 182,
        [Description("Oxford, United Kingdom")]
        OxfordUnitedKingdom = 183,
        [Description("Portsmouth, United Kingdom")]
        PortsmouthUnitedKingdom = 184,
        [Description("Reading, United Kingdom")]
        ReadingUnitedKingdom = 185,
        [Description("Redhill, United Kingdom")]
        RedhillUnitedKingdom = 186,
        [Description("Sheffield, United Kingdom")]
        SheffieldUnitedKingdom = 187,
        [Description("Slough, United Kingdom")]
        SloughUnitedKingdom = 188,
        [Description("Southampton, United Kingdom")]
        SouthamptonUnitedKingdom = 189,
        [Description("Tonbridge, United Kingdom")]
        TonbridgeUnitedKingdom = 190,
        [Description("LatinAmerica")]
        LatinAmerica = 191,
        [Description("Argentina")]
        Argentina = 192,
        [Description("Bolivia")]
        Bolivia = 193,
        [Description("Brazil")]
        Brazil = 194,
        [Description("Acre")]
        Acre = 195,
        [Description("Alagoas")]
        Alagoas = 196,
        [Description("Amapá")]
        Amapá = 197,
        [Description("Amazonas")]
        Amazonas = 198,
        [Description("Bahia")]
        Bahia = 200,
        [Description("Ceará")]
        Ceará = 201,
        [Description("Distrito Federal")]
        DistritoFederal = 202,
        [Description("Espírito Santo")]
        EspíritoSanto = 203,
        [Description("Goiás")]
        Goiás = 204,
        [Description("Maranhão")]
        Maranhão = 205,
        [Description("Mato Grosso")]
        MatoGrosso = 206,
        [Description("Mato Grosso do Sul")]
        MatoGrossodoSul = 207,
        [Description("Minas Gerais")]
        MinasGerais = 208,
        [Description("Belo Horizonte Area, Brazil")]
        BeloHorizonteAreaBrazil = 209,
        [Description("Paraná")]
        Paraná = 210,
        [Description("Curitiba Area, Brazil")]
        CuritibaAreaBrazil = 211,
        [Description("Paraíba")]
        Paraíba = 212,
        [Description("Pará")]
        Pará = 213,
        [Description("Pernambuco")]
        Pernambuco = 214,
        [Description("Piauí")]
        Piauí = 215,
        [Description("Rio Grande do Norte")]
        RioGrandedoNorte = 216,
        [Description("Rio Grande do Sul")]
        RioGrandedoSul = 217,
        [Description("Porto Alegre Area, Brazil")]
        PortoAlegreAreaBrazil = 218,
        [Description("Rio de Janeiro")]
        RiodeJaneiro = 219,
        [Description("Rio de Janeiro Area, Brazil")]
        RiodeJaneiroAreaBrazil = 220,
        [Description("Rondônia")]
        Rondônia = 221,
        [Description("Roraima")]
        Roraima = 222,
        [Description("Santa Catarina")]
        SantaCatarina = 223,
        [Description("Sergipe")]
        Sergipe = 224,
        [Description("São Paulo")]
        SãoPaulo = 225,
        [Description("Campinas Area, Brazil")]
        CampinasAreaBrazil = 226,
        [Description("São Paulo Area, Brazil")]
        SãoPauloAreaBrazil = 227,
        [Description("Tocantins")]
        Tocantins = 228,
        [Description("Chile")]
        Chile = 229,
        [Description("Colombia")]
        Colombia = 230,
        [Description("CostaRica")]
        CostaRica = 231,
        [Description("Dominican Republic")]
        DominicanRepublic = 232,
        [Description("Ecuador")]
        Ecuador = 234,
        [Description("Guatemala")]
        Guatemala = 235,
        [Description("Mexico")]
        Mexico = 236,
        [Description("Mexico City Area, Mexico")]
        MexicoCityAreaMexico = 237,
        [Description("Naucalpan de Juárez Area, Mexico")]
        NaucalpandeJuárezAreaMexico = 238,
        [Description("Panama")]
        Panama = 239,
        [Description("Peru")]
        Peru = 240,
        [Description("Puerto Rico")]
        PuertoRico = 241,
        [Description("Trinidad and Tobago")]
        TrinidadandTobago = 242,
        [Description("Uruguay")]
        Uruguay = 243,
        [Description("Venezuela")]
        Venezuela = 244,
        [Description("Middle East")]
        MiddleEast = 245,
        [Description("Bahrain")]
        Bahrain = 246,
        [Description("Israel")]
        Israel = 247,
        [Description("Jordan")]
        Jordan = 248,
        [Description("Kuwait")]
        Kuwait = 249,
        [Description("Pakistan")]
        Pakistan = 250,
        [Description("Qatar")]
        Qatar = 251,
        [Description("Saudi Arabia")]
        SaudiArabia = 252,
        [Description("United Arab Emirates")]
        UnitedArabEmirates = 253,
        [Description("North America")]
        NorthAmerica = 254,
        [Description("Canada")]
        Canada = 255,
        [Description("Alberta")]
        Alberta = 256,
        [Description("Calgary, Canada Area")]
        CalgaryCanadaArea = 257,
        [Description("Edmonton, Canada Area")]
        EdmontonCanadaArea = 258,
        [Description("British Columbia")]
        BritishColumbia = 259,
        [Description("British Columbia, Canada")]
        BritishColumbiaCanada = 260,
        [Description("Vancouver, Canada Area")]
        VancouverCanadaArea = 261,
        [Description("Manitoba")]
        Manitoba = 262,
        [Description("New Brunswick")]
        NewBrunswick = 263,
        [Description("Newfoundland And Labrador")]
        NewfoundlandAndLabrador = 264,
        [Description("Northwest Territories")]
        NorthwestTerritories = 265,
        [Description("Nova Scotia")]
        NovaScotia = 266,
        [Description("Halifax, Canada Area")]
        HalifaxCanadaArea = 267,
        [Description("Nunavut")]
        Nunavut = 268,
        [Description("Ontario")]
        Ontario = 269,
        [Description("Kitchener, Canada Area")]
        KitchenerCanadaArea = 270,
        [Description("London, Canada Area")]
        LondonCanadaArea = 271,
        [Description("Ontario, Canada")]
        OntarioCanada = 272,
        [Description("Toronto, Canada Area")]
        TorontoCanadaArea = 273,
        [Description("Prince Edward Island")]
        PrinceEdwardIsland = 274,
        [Description("Quebec")]
        Quebec = 275,
        [Description("Montreal, Canada Area")]
        MontrealCanadaArea = 276,
        [Description("Ottawa, Canada Area")]
        OttawaCanadaArea = 277,
        [Description("Quebec, Canada")]
        QuebecCanada = 278,
        [Description("Winnipeg, Canada Area")]
        WinnipegCanadaArea = 279,
        [Description("Saskatchewan")]
        Saskatchewan = 280,
        [Description("Yukon")]
        Yukon = 281,
        [Description("United States")]
        UnitedStates = 282,
        [Description("Alabama")]
        Alabama = 283,
        [Description("Birmingham, Alabama Area")]
        BirminghamAlabamaArea = 284,
        [Description("Alaska")]
        Alaska = 285,
        [Description("Anchorage, Alaska Area")]
        AnchorageAlaskaArea = 286,
        [Description("Arizona")]
        Arizona = 287,
        [Description("Phoenix, Arizona Area")]
        PhoenixArizonaArea = 288,
        [Description("Tucson, Arizona Area")]
        TucsonArizonaArea = 289,
        [Description("Arkansas")]
        Arkansas = 290,
        [Description("Little Rock, Arkansas Area")]
        LittleRockArkansasArea = 291,
        [Description("California")]
        California = 292,
        [Description("Fresno, California Area")]
        FresnoCaliforniaArea = 293,
        [Description("Greater Los Angeles Area")]
        GreaterLosAngelesArea = 294,
        [Description("Greater San Diego Area")]
        GreaterSanDiegoArea = 295,
        [Description("Orange County, California Area")]
        OrangeCountyCaliforniaArea = 296,
        [Description("Sacramento, California Area")]
        SacramentoCaliforniaArea = 297,
        [Description("Salinas, California Area")]
        SalinasCaliforniaArea = 298,
        [Description("San Francisco Bay Area")]
        SanFranciscoBayArea = 299,
        [Description("Santa Barbara, California Area")]
        SantaBarbaraCaliforniaArea = 300,
        [Description("Stockton, California Area")]
        StocktonCaliforniaArea = 301,
        [Description("Colorado")]
        Colorado = 302,
        [Description("Colorado Springs, Colorado Area")]
        ColoradoSpringsColoradoArea = 304,
        [Description("Fort Collins, Colorado Area")]
        FortCollinsColoradoArea = 305,
        [Description("Greater Denver Area")]
        GreaterDenverArea = 306,
        [Description("Connecticut")]
        Connecticut = 307,
        [Description("Hartford, Connecticut Area")]
        HartfordConnecticutArea = 308,
        [Description("New London/Norwich, Connecticut Area")]
        NewLondonNorwichConnecticutArea = 309,
        [Description("Delaware")]
        Delaware = 310,
        [Description("District Of Columbia")]
        DistrictOfColumbia = 311,
        [Description("Washington D.C. Metro Area")]
        WashingtonDCMetroArea = 312,
        [Description("Florida")]
        Florida = 313,
        [Description("Daytona Beach, Florida Area")]
        DaytonaBeachFloridaArea = 314,
        [Description("Fort Myers, Florida Area")]
        FortMyersFloridaArea = 315,
        [Description("Fort Pierce, Florida Area")]
        FortPierceFloridaArea = 316,
        [Description("Gainesville, Florida Area")]
        GainesvilleFloridaArea = 317,
        [Description("Lakeland, Florida Area")]
        LakelandFloridaArea = 318,
        [Description("Melbourne, Florida Area")]
        MelbourneFloridaArea = 319,
        [Description("Miami/Fort Lauderdale Area")]
        MiamiFortLauderdaleArea = 320,
        [Description("Orlando, Florida Area")]
        OrlandoFloridaArea = 321,
        [Description("Sarasota, Florida Area")]
        SarasotaFloridaArea = 322,
        [Description("Tampa/St. Petersburg, Florida Area")]
        TampaStPetersburgFloridaArea = 323,
        [Description("West Palm Beach, Florida Area")]
        WestPalmBeachFloridaArea = 324,
        [Description("Georgia")]
        Georgia = 3258,
        [Description("Greater Atlanta Area")]
        GreaterAtlantaArea = 326,
        [Description("Jacksonville, Florida Area")]
        JacksonvilleFloridaArea = 327,
        [Description("Tallahassee, Florida Area")]
        TallahasseeFloridaArea = 328,
        [Description("Hawaii")]
        Hawaii = 329,
        [Description("Hawaiian Islands")]
        HawaiianIslands = 340,
        [Description("Idaho")]
        Idaho = 341,
        [Description("Boise, Idaho Area")]
        BoiseIdahoArea = 342,
        [Description("Illinois")]
        Illinois = 343,
        [Description("Greater Chicago Area")]
        GreaterChicagoArea = 344,
        [Description("Peoria, Illinois Area")]
        PeoriaIllinoisArea = 345,
        [Description("Urbana-Champaign, Illinois Areaa")]
        UrbanaChampaignIllinoisArea = 346,
        [Description("Indiana")]
        Indiana = 347,
        [Description("Evansville, Indiana Area")]
        EvansvilleIndianaArea = 348,
        [Description("Indianapolis, Indiana Area")]
        IndianapolisIndianaArea = 349,
        [Description("Iowa")]
        Iowa = 350,
        [Description("Kansas")]
        Kansas = 351,
        [Description("Wichita, Kansas Area")]
        WichitaKansasArea = 352,
        [Description("Kentucky")]
        Kentucky = 353,
        [Description("Lexington, Kentucky Area")]
        LexingtonKentuckyArea = 354,
        [Description("Louisville, Kentucky Area")]
        LouisvilleKentuckyArea = 355,
        [Description("Louisiana")]
        Louisiana = 356,
        [Description("Maine")]
        Maine = 357,
        [Description("Portland, Maine Area")]
        PortlandMaineArea = 358,
        [Description("Maryland")]
        Maryland = 359,
        [Description("Baltimore, Maryland Area")]
        BaltimoreMarylandArea = 360,
        [Description("Massachusetts")]
        Massachusetts = 361,
        [Description("Greater Boston Area")]
        GreaterBostonArea = 362,
        [Description("Michigan")]
        Michigan = 363,
        [Description("Fort Wayne, Indiana Area")]
        FortWayneIndianaArea = 364,
        [Description("Greater Detroit Area")]
        GreaterDetroitArea = 365,
        [Description("Greater Grand Rapids, Michigan Area")]
        GreaterGrandRapidsMichiganArea = 366,
        [Description("Kalamazoo, Michigan Area")]
        KalamazooMichiganArea = 367,
        [Description("Lansing, Michigan Area")]
        LansingMichiganArea = 368,
        [Description("Saginaw, Michigan Area")]
        SaginawMichiganArea = 369,
        [Description("Minnesota")]
        Minnesota = 370,
        [Description("Greater Minneapolis-St. Paul Area")]
        GreaterMinneapolisStPaulArea = 371,
        [Description("Mississippi")]
        Mississippi = 372,
        [Description("Baton Rouge, Louisiana Area")]
        BatonRougeLouisianaArea = 373,
        [Description("Greater New Orleans Area")]
        GreaterNewOrleansArea = 374,
        [Description("Jackson, Mississippi Area")]
        JacksonMississippiArea = 375,
        [Description("Mobile, Alabama Area")]
        MobileAlabamaArea = 376,
        [Description("Missouri")]
        Missouri = 377,
        [Description("Columbia, Missouri Area")]
        ColumbiaMissouriArea = 378,
        [Description("Davenport, Iowa Area")]
        DavenportIowaArea = 379,
        [Description("Des Moines, Iowa Area")]
        DesMoinesIowaArea = 380,
        [Description("Fayetteville, Arkansas Area")]
        FayettevilleArkansasArea = 381,
        [Description("Greater St. Louis Area")]
        GreaterStLouisArea = 382,
        [Description("Kansas City, Missouri Area")]
        KansasCityMissouriArea = 383,
        [Description("Springfield, Missouri Area")]
        SpringfieldMissouriArea = 384,
        [Description("Montana")]
        Montana = 385,
        [Description("Nebraska")]
        Nebraska = 386,
        [Description("Greater Omaha Area")]
        GreaterOmahaArea = 387,
        [Description("Lincoln, Nebraska Area")]
        LincolnNebraskaArea = 388,
        [Description("Nevada")]
        Nevada = 389,
        [Description("Las Vegas, Nevada Area")]
        LasVegasNevadaArea = 390,
        [Description("Reno, Nevada Area")]
        RenoNevadaArea = 391,
        [Description("New Hampshire")]
        NewHampshire = 392,
        [Description("New Jersey")]
        NewJersey = 393,
        [Description("New Mexico")]
        NewMexico = 394,
        [Description("Albuquerque, New Mexico Area")]
        AlbuquerqueNewMexicoArea = 395,
        [Description("New York")]
        NewYork = 396,
        [Description("Albany, New York Area")]
        AlbanyNewYorkArea = 397,
        [Description("Buffalo/Niagara, New York Area")]
        BuffaloNiagaraNewYorkArea = 398,
        [Description("Greater New York City Area")]
        GreaterNewYorkCityArea = 399,
        [Description("Rochester, New York Area")]
        RochesterNewYorkArea = 400,
        [Description("Syracuse, New York Area")]
        SyracuseNewYorkArea = 401,
        [Description("North Carolina")]
        NorthCarolina = 402,
        [Description("Charlotte, North Carolina Area")]
        CharlotteNorthCarolinaArea = 403,
        [Description("Raleigh-Durham, North Carolina Area")]
        RaleighDurhamNorthCarolinaArea = 404,
        [Description("Wilmington, North Carolina Area")]
        WilmingtonNorthCarolinaArea = 405,
        [Description("North Dakota")]
        NorthDakota = 406,
        [Description("Ohio")]
        Ohio = 407,
        [Description("Cincinnati Area")]
        CincinnatiArea = 408,
        [Description("Cleveland/Akron, Ohio Area")]
        ClevelandAkronOhioArea = 409,
        [Description("Columbus, Ohio Area")]
        ColumbusOhioArea = 410,
        [Description("Dayton, Ohio Area")]
        DaytonOhioArea = 411,
        [Description("Toledo, Ohio Area")]
        ToledoOhioArea = 412,
        [Description("Oklahoma")]
        Oklahoma = 413,
        [Description("Oklahoma City, Oklahoma Area")]
        OklahomaCityOklahomaArea = 414,
        [Description("Tulsa, Oklahoma Area")]
        TulsaOklahomaArea = 415,
        [Description("Oregon")]
        Oregon = 416,
        [Description("Eugene, Oregon Area")]
        EugeneOregonArea = 417,
        [Description("Portland, Oregon Area")]
        PortlandOregonArea = 418,
        [Description("Pennsylvania")]
        Pennsylvania = 419,
        [Description("Allentown, Pennsylvania Area")]
        AllentownPennsylvaniaArea = 420,
        [Description("Greater Philadelphia Area")]
        GreaterPhiladelphiaArea = 421,
        [Description("Greater Pittsburgh Area")]
        GreaterPittsburghArea = 422,
        [Description("Harrisburg, Pennsylvania Area")]
        HarrisburgPennsylvaniaArea = 423,
        [Description("Ithaca, New York Area")]
        IthacaNewYorkArea = 424,
        [Description("Lancaster, Pennsylvania Area")]
        LancasterPennsylvaniaArea = 425,
        [Description("Scranton, Pennsylvania Area")]
        ScrantonPennsylvaniaArea = 426,
        [Description("Rhode Island")]
        RhodeIsland = 427,
        [Description("Providence, Rhode Island Area")]
        ProvidenceRhodeIslandArea = 428,
        [Description("South Carolina")]
        SouthCarolina = 429,
        [Description("Augusta, Georgia Area")]
        AugustaGeorgiaArea = 430,
        [Description("Charleston, South Carolina Area")]
        CharlestonSouthCarolinaArea = 431,
        [Description("Columbia, South Carolina Area")]
        ColumbiaSouthCarolinaArea = 432,
        [Description("Greenville, South Carolina Area")]
        GreenvilleSouthCarolinaArea = 433,
        [Description("Savannah, Georgia Area")]
        SavannahGeorgiaArea = 434,
        [Description("South Dakota")]
        SouthDakota = 435,
        [Description("Sioux Falls, South Dakota Area")]
        SiouxFallsSouthDakotaArea = 436,
        [Description("Tennessee")]
        Tennessee = 437,
        [Description("Asheville, North Carolina Area")]
        AshevilleNorthCarolinaArea = 438,
        [Description("Chattanooga, Tennessee Area")]
        ChattanoogaTennesseeArea = 439,
        [Description("Greater Memphis Area")]
        GreaterMemphisArea = 440,
        [Description("Greater Nashville Area")]
        GreaterNashvilleArea = 441,
        [Description("Huntsville, Alabama Area")]
        HuntsvilleAlabamaArea = 412,
        [Description("Johnson City, Tennessee Area")]
        JohnsonCityTennesseeArea = 413,
        [Description("Knoxville, Tennessee Area")]
        KnoxvilleTennesseeArea = 444,
        [Description("Texas")]
        Texas = 445,
        [Description("Austin, Texas Area")]
        AustinTexasArea = 446,
        [Description("Dallas/Fort Worth Area")]
        DallasFortWorthArea = 447,
        [Description("El Paso, Texas Area")]
        ElPasoTexasArea = 448,
        [Description("Houston, Texas Area")]
        HoustonTexasArea = 449,
        [Description("San Antonio, Texas Area")]
        SanAntonioTexasArea = 450,
        [Description("Utah")]
        Utah = 451,
        [Description("Greater Salt Lake City Area")]
        GreaterSaltLakeCityArea = 452,
        [Description("Provo, Utah Area")]
        ProvoUtahArea = 453,
        [Description("Vermont")]
        Vermont = 454,
        [Description("Burlington, Vermont Area")]
        BurlingtonVermontArea = 455,
        [Description("Springfield, Massachusetts Area")]
        Springfield, MassachusettsArea = 456,
        [Description("Virginia")]
        Virginia = 457,
        [Description("Charlottesville, Virginia Area")]
        CharlottesvilleVirginiaArea = 458,
        [Description("Greensboro/Winston-Salem, North Carolina Area")]
        GreensboroWinstonSalemNorthCarolinaArea = 459,
        [Description("Norfolk, Virginia Area")]
        NorfolkVirginiaArea = 460,
        [Description("Richmond, Virginia Area")]
        RichmondVirginiaArea = 461,
        [Description("Roanoke, Virginia Area")]
        RoanokeVirginiaArea = 462,
        [Description("Washington")]
        Washington = 463,
        [Description("Greater Seattle Area")]
        GreaterSeattleArea = 464,
        [Description("Spokane, Washington Area")]
        SpokaneWashingtonArea = 465,
        [Description("West Virginia")]
        WestVirginia = 456,
        [Description("Wisconsin")]
        Wisconsin = 457,
        [Description("Greater Milwaukee Area")]
        GreaterMilwaukeeArea = 458,
        [Description("Green Bay, Wisconsin Area")]
        GreenBayWisconsinArea = 459,
        [Description("Madison, Wisconsin Area")]
        MadisonWisconsinArea = 460,
        [Description("Oshkosh, Wisconsin Area")]
        OshkoshWisconsinArea = 461,
        [Description("Rockford, Illinois Area")]
        RockfordIllinoisArea = 462,
        [Description("Wyoming")]
        Wyoming = 463,
        [Description("Oceania")]
        Oceania = 464,
        [Description("Australia")]
        Australia = 465,
        [Description("Adelaide Area, Australia")]
        AdelaideAreaAustralia = 466,
        [Description("Brisbane Area, Australia")]
        BrisbaneAreaAustralia = 467,
        [Description("Canberra Area, Australia")]
        CanberraAreaAustralia = 468,
        [Description("Melbourne Area, Australia")]
        MelbourneAreaAustralia = 469,
        [Description("Perth Area, Australia")]
        PerthAreaAustralia = 470,
        [Description("Sydney Area, Australia")]
        SydneyAreaAustralia = 471,
        [Description("New Zealand")]
        NewZealand = 472,
    }

    //===================Skill Enum Values============================

    public enum SkillCategory
    {
        [Description("Consulting")]
        Consulting = 1,
        [Description("Enterprise App Development")]
        EnterpriseAppdevelopment = 2,
        [Description("Blockchain")]
        Blockchain = 3,
        [Description("Cloud")]
        Cloud = 4,
        [Description("Cognitive Services")]
        CognitiveServices = 5,
        [Description("AI/MachineLearning")]
        ALMachineLearning = 6,
        [Description("Cloud")]
        Devops = 7,
        [Description("IOT")]
        IOT = 8,
        [Description("Data Analytics and Insights")]
        DataAnalyticsandInsights = 9,
        [Description("Infrastructure")]
        Infrastructure = 10,
        [Description("Cybersecurity")]
        Cybersecurity = 11,
        [Description("Enterprise Automation")]
        EnterpriseAutomation = 12,
        [Description("Quality Engineering")]
        QualityEngineering = 13,
        [Description("Industrial Engineering")]
        IndustrialEngineering = 14,
        [Description("SoftWare Product Engineering")]
        SoftWareProductEngineering = 16,
    }
    public enum SkillIP
    {
        [Description("Proprietary")]
        Proprietary = 1,
        [Description("ISV")]
        ISV = 2,
        [Description("Open Source")]
        OpenSource = 3
    }
    public enum SkillExpertiseLevel
    {
        [Description("Beginner")]
        Beginner = 1,
        [Description("Intermediate")]
        Intermediate = 2,
        [Description("Professional")]
        Professional = 3,
        [Description("Expert")]
        Expert = 4,
        [Description("Scholar")]
        Scholar = 5
    }
    public enum SkillYearsOfExperience
    {
        [Description("1-3")]
        OneToThree = 1,
        [Description("3-7")]
        ThreeToSeven = 2,
        [Description("7-10")]
        SevenToTez = 3,
        [Description("10-15")]
        TenToFifteen = 4,
        [Description("15-17")]
        FifteenToSeventeen = 5,
        [Description("17-20")]
        SeventeenToTwenty = 6,
        [Description("20-25")]
        TwentyToTwentyFive = 7,
        [Description("25+")]
        TwentyFivePlus = 8
    }
    public enum SkillNature
    {
        [Description("Consultancy")]
        Consultancy = 1,
        [Description(" Business Analysis")]
        BusinessAnalysis = 2,
        [Description("UX/Solutoining")]
        UXSolutoining = 3,
        [Description("Development")]
        Development = 4,
        [Description("OA")]
        OA = 5,
        [Description("Deployment")]
        Deployment = 6,
        [Description("Platform Support-Software")]
        PlatformSupportSoftware = 7,
        [Description("Platform Support-Hardware")]
        PlatformSupportHardware = 8,
        [Description("Administration")]
        Administration = 9,
        [Description("Relase And Coordination")]
        RelaseAndCoordination = 10
    }


    public enum DayTimeSlot
    {
        [Description("Morning")]
        Morning = 1,
        [Description("Evening")]
        Evening = 2,
        [Description("All")]
        All = 3
    }

    public enum DaySlot
    {
        [Description("Monday")]
        Monday = 1,
        [Description("Tuesday")]
        Tuesday = 2,
        [Description("Wednesday")]
        Wednesday = 3,
        [Description("Thursday")]
        Thursday = 4,
        [Description("Friday")]
        Friday = 5,
        [Description("Saturday")]
        Saturday = 6,
        [Description("Sunday")]
        Sunday = 7,
        [Description("All")]
        All = 8,

    }
    public enum InterviewerSkillsLevel
    {
        [Description("Competent")]
        Competent = 1,
        [Description("Proficient")]
        Proficient = 2,
        [Description("Expert")]
        Expert = 3
    }
    public enum CompetencyLevel
    {
        [Description("Novice")]
        Novice = 1,
        [Description("Beginner")]
        Beginner = 2,
        [Description("Competent")]
        Competent = 3,
        [Description("Proficient")]
        Proficient = 4,
        [Description("Expert")]
        Expert = 5
    }
    public enum Heuristicsscorelevel
    {
        [Description("Basic")]
        Basic = 1,
        [Description("Standard")]
        Standard = 2,
        [Description("Standard+")]
        StandardPlus = 3,
        [Description("Pro")]
        Pro = 4,
        [Description("Elite")]
        Elite = 5
    }
    public enum AssassinationStatus
    {
        [Description("Assigned")]
        Assigned,
        [Description("Resolved")]
        Resolved
    }
    public enum InterviewRequestCountry
    {
        [Description("India")]
        India = 1,
        [Description("Outside India")]
        OutsideIndia = 2
    }
    public enum LeadInterviewerenum
    {
        [Description("True")]
        True = 1,
        [Description("False")]
        False = 0
    }
}

