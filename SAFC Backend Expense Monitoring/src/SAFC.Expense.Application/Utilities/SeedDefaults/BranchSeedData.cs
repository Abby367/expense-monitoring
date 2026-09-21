namespace SAFC.Expense.Application.Utilities.SeedDefaults;

/// <summary>
/// Canonical SAFC branch reference data used to seed the <c>Branches</c> table.
/// </summary>
/// <remarks>
/// Source: the <c>Sheet2</c> worksheet of <c>List of Branches.xlsx</c>, rows 2 to 92, and
/// nothing else. That worksheet is the only one carrying a branch code per row, so it is the
/// sole source of truth. A branch appears below only if that sheet lists it.
///
/// - <see cref="BranchSeedRecord.Code"/> is <c>BRAN_CODE</c>, the stable business key. It is
///   never rewritten: role grants are scoped to a branch, so a code that changed would silently
///   re-point every grant that named it.
/// - <see cref="BranchSeedRecord.Name"/> is <c>BRAN_NAME</c>, transcribed verbatim. The source
///   casing is inconsistent ("Head office", "Common Wealth") and is deliberately left alone so
///   this system and onboarding display the same string.
///
/// DELIBERATELY NOT CARRIED OVER, so a reader comparing this with onboarding's equivalent file
/// does not assume an oversight:
/// - <c>Address</c> - <see cref="SAFC.Expense.Domain.Entities.Branch"/> has no address column.
///   Nothing in expense monitoring reads a branch address.
/// - The SAFC area grouping ("NCR SOUTH", "SOUTH LUZON 2"). Onboarding carries it in a column
///   misleadingly named <c>Url</c>. It is dropped here because nothing reads it: a grant is
///   scoped to a single branch, so an Area Supervisor holds several branch-scoped grants rather
///   than one area-scoped grant. Area is therefore a UI convenience for issuing grants in bulk,
///   not a scope type. The workbook has no area column either - onboarding carried it over from
///   previously seeded values - so if it is ever needed, re-source it from onboarding's
///   <c>BranchSeedData.Branches[].Url</c>, not from the workbook.
/// - <c>IsSeeded</c> - onboarding needs it because branches can also be created through its API,
///   so its populator must know which rows it owns. Here branches are seeded and never
///   API-created, so every row is seed-owned by construction and the flag would have one value
///   forever.
///
/// Codes look numeric but are not: 88 are "101".."262" with real gaps in the range, and three
/// are alphabetic - INT, MKT and ROX. Never parse or sort them as integers.
///
/// "101" is Head office rather than a branch. Whether a role grant scoped to 101 is meaningful,
/// or whether head office is what an org-wide grant means, is a BranchScope decision and is not
/// settled here.
/// </remarks>
public static class BranchSeedData
{
    /// <summary>A single branch to seed. Matching is on <paramref name="Code"/>.</summary>
    /// <param name="Code">Stable branch code owned by the source system.</param>
    /// <param name="Name">Branch display name, verbatim from the workbook.</param>
    public sealed record BranchSeedRecord(string Code, string Name);

    /// <summary>The 91 branches listed by the workbook.</summary>
    public static IReadOnlyList<BranchSeedRecord> Branches { get; } =
    [
        new("101",  "Head office"),
        new("102",  "Sta. Rosa"),
        new("103",  "Lucena"),
        new("104",  "Legazpi"),
        new("105",  "Batangas"),
        new("106",  "Naga"),
        new("107",  "Sta. Cruz"),
        new("108",  "Lipa"),
        new("109",  "San Pablo"),
        new("110",  "Daet"),
        new("111",  "Mandaue"),
        new("112",  "Lemery"),
        new("114",  "Balayan"),
        new("115",  "Bauan"),
        new("116",  "Calapan"),
        new("117",  "San Jose"),
        new("118",  "Tanauan"),
        new("119",  "Rosario"),
        new("120",  "San Pedro"),
        new("121",  "Biñan"),
        new("122",  "Calamba"),
        new("123",  "Candelaria"),
        new("127",  "Iriga"),
        new("128",  "Pili"),
        new("129",  "Goa"),
        new("131",  "Sorsogon"),
        new("138",  "Caloocan"),
        new("146",  "Pinamalayan"),
        new("147",  "Roxas Oriental Mindoro"),
        new("164",  "Cabuyao"),
        new("171",  "Pagbilao"),
        new("172",  "Bacoor"),
        new("173",  "Las Piñas"),
        new("174",  "Plaridel"),
        new("175",  "Sta. Maria"),
        new("177",  "Angeles"),
        new("178",  "Novaliches"),
        new("179",  "Valenzuela"),
        new("180",  "Taguig"),
        new("181",  "Parañaque"),
        new("182",  "Pasay"),
        new("183",  "Balanga"),
        new("184",  "Olongapo"),
        new("185",  "Gapan"),
        new("186",  "Tarlac"),
        new("187",  "Urdaneta"),
        new("190",  "Antipolo"),
        new("191",  "Cainta"),
        new("192",  "Common Wealth"),
        new("193",  "Marikina"),
        new("194",  "San Juan"),
        new("195",  "Mandaluyong"),
        new("196",  "Sariaya"),
        new("197",  "Sto. Tomas"),
        new("198",  "Timog Quezon City"),
        new("200",  "San Fernando Pampanga"),
        new("201",  "Malabon"),
        new("203",  "Imus"),
        new("204",  "Meycauayan"),
        new("205",  "San Jose Del Monte"),
        new("206",  "Tabaco"),
        new("207",  "Baliwag"),
        new("209",  "Dasmariñas Cavite"),
        new("210",  "Lian"),
        new("211",  "Los Baños"),
        new("213",  "Dagupan"),
        new("215",  "Cabanatuan"),
        new("216",  "Muntinlupa"),
        new("220",  "San Fernando La Union"),
        new("221",  "Vigan"),
        new("222",  "Tuguegarao"),
        new("223",  "Santiago"),
        new("224",  "Gumaca"),
        new("225",  "Solano"),
        new("226",  "Laoag"),
        new("237",  "Congressional Avenue"),
        new("238",  "Cebu"),
        new("251",  "Bacolod"),
        new("252",  "Zamboanga"),
        new("253",  "General Santos"),
        new("254",  "Paniqui"),
        new("255",  "Iligan"),
        new("256",  "Polangui"),
        new("257",  "Capas"),
        new("258",  "Malaybalay"),
        new("259",  "Pagadian"),
        new("261",  "Malolos"),
        new("262",  "Agoo"),
        new("INT",  "Intramuros"),
        new("MKT",  "Makati"),
        new("ROX",  "Manila"),
    ];
}
