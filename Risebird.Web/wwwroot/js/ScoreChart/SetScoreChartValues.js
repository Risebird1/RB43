

function SetChart() {
    var arr = [];
    var obj = { y: "", legendText: "", label: "" }
    //TechSkills actual weightage total
    var techItems = $('[id^="TechSkillReportCollection"][id$="WeightageActual"]');
    var techTotal = 0;
    for (i = 0; i < techItems.length; i++) {

        techTotal = parseInt(techTotal) + parseInt($("#TechSkillReportCollection_" + i + "__WeightageActual").val());

    }
    //obj.y = parseInt(techTotal) / 4;
    obj.y = parseInt(techTotal);
    obj.label = "Tech Skills Score";
    obj.legendText = "Tech Skills Score";
    arr.push(obj.y);

    //SkillsGeneral actual weightage total
    var objGen = { y: "", legendText: "", label: "" }
    var genSkillsItems = $('[id^="SkillGeneralReportCollection"][id$="WeightageActual"]');
    var genTotal = 0;
    for (i = 0; i < genSkillsItems.length; i++) {
        var weightage = $("#SkillGeneralReportCollection_" + i + "__WeightageActual").val();
        genTotal = parseInt(genTotal) + parseInt(weightage);

    }

    //objGen.y = parseInt(genTotal) / 4;
    objGen.y = parseInt(genTotal);
    objGen.label = "General Skills Score";
    objGen.legendText = "General Skills Score";
    arr.push(objGen.y);

    //Joinability actual weightage total
    var objJoin = { y: "", legendText: "", label: "" }
    var joinabilityItems = $('[id^="JoinabilityHRReportCollection"][id$="WeightageActual"]');
    var joinabilityTotal = 0;
    for (i = 0; i < joinabilityItems.length; i++) {

        joinabilityTotal = parseInt(joinabilityTotal) + parseInt($("#JoinabilityHRReportCollection_" + i + "__WeightageActual").val());

    }
    //objJoin.y = parseInt(joinabilityTotal) / 4;
    objJoin.y = parseInt(joinabilityTotal);
    objJoin.label = "Joinability Score";
    objJoin.legendText = "Joinability Score";
    arr.push(objJoin.y);

    //Selectability actual weightage total
    var objSelect = { y: "", legendText: "", label: "" }
    var selectabilityItems = $('[id^="SelectabilityEvaluatorReportCollection"][id$="WeightageActual"]');
    var selectabilityTotal = 0;
    for (i = 0; i < selectabilityItems.length; i++) {

        selectabilityTotal = parseInt(selectabilityTotal) + parseInt($("#SelectabilityEvaluatorReportCollection_" + i + "__WeightageActual").val());

    }
    //objSelect.y = parseInt(selectabilityTotal) / 4;
    objSelect.y = parseInt(selectabilityTotal);
    objSelect.label = "Selectability Score";
    objSelect.legendText = "Selectability Score";
    arr.push(objSelect.y);

    //totalScore = (techTotal + genTotal + joinabilityTotal + selectabilityTotal) / 4;
    //document.getElementById("totScore").innerHTML = average;
    document.getElementById("totTechScore").innerHTML = techTotal;

    am4core.ready(function () {

        // Themes begin
        am4core.useTheme(am4themes_animated);
        // Themes end

        // Create chart instance
        var chart = am4core.create("scoreChart", am4charts.XYChart);

        // Add data
        chart.data = [{
            "country": "Tech",
            "visits": arr[0]

        }, {
            "country": "General",
            "visits": arr[1]
        }, {
            "country": "Joinability",
            "visits": arr[2]
        }, {
            "country": "Selectability",
            "visits": arr[3]
        }];

        // Create axes
        chart.logo.disabled = true;
        var categoryAxis = chart.xAxes.push(new am4charts.CategoryAxis());
        categoryAxis.dataFields.category = "country";
        categoryAxis.renderer.inside = true;
        categoryAxis.renderer.labels.template.valign = 0;
        categoryAxis.renderer.grid.template.location = 0;
        //categoryAxis.renderer.minGridDistance = 30;
        categoryAxis.renderer.minGridDistance = 100;
        chart.fontSize = 13;
        categoryAxis.renderer.labels.template.fill = am4core.color("#828080");

        categoryAxis.renderer.labels.template.adapter.add("dy", function (dy, target) {
            if (target.dataItem && target.dataItem.index & 2 == 2) {
                return dy;
            }
            return dy;
        });
        categoryAxis.renderer.labels.template.fill = am4core.color("#828080");
        categoryAxis.renderer.labels.template.paddingBottom = -17;
        categoryAxis.renderer.grid.template.disabled = true;
        var valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
        valueAxis.renderer.grid.template.disabled = true;
        valueAxis.min = 0;
        //valueAxis.max = 30;
        valueAxis.max = 100;
        // Create series
        var series = chart.series.push(new am4charts.ColumnSeries());
        series.dataFields.valueY = "visits";
        series.dataFields.categoryX = "country";
        series.name = "Visits";
        series.colorField = "color";
        series.columns.template.tooltipText = "{valueY.percent}[/] ({valueY})";
        series.tooltip.label.adapter.add("text", function (text, target) {
            if (target.dataItem && target.dataItem.valueY == 0) {

                return "";
            }
            else {

                return text;
            }
        });
        series.columns.template.fillOpacity = .8;
        series.columns.template.tooltipY = 0;
        series.tooltip.background.strokeOpacity = 0
        series.tooltip.pointerOrientation = "vertical";
        series.columns.template.showTooltipOn = "always";
        series.tooltip.background.filters.clear();
        series.tooltip.getFillFromObject = false;

        series.tooltip.label.fill = am4core.color("#9a9a9a");
        series.tooltip.background.cornerRadius = 0;
        series.tooltip.label.textAlign = "middle";
        series.tooltip.background.fill = 0;
        var columnTemplate = series.columns.template;
        columnTemplate.strokeWidth = 0;
        columnTemplate.strokeOpacity = 1;//07BEF3
        chart.colors.list = [
            am4core.color("#07BEF3"),
            am4core.color("#00F96B"),
            am4core.color("#F64042"),
            am4core.color("#ffbc50")

        ];
        series.columns.template.events.once("inited", function (event) {
            event.target.fill = chart.colors.getIndex(event.target.dataItem.index);
        });

    });
}

function SetChartForVideoPage() {
    var arr = [];
    var obj = { y: "", legendText: "", label: "" }
    //TechSkills actual weightage total
    var techItems = $('[id^="IreportModel_TechSkillReportCollection"][id$="WeightageActual"]');
    var techTotal = 0;
    for (i = 0; i < (techItems.length / 2); i++) {

        techTotal = parseInt(techTotal) + parseInt($("#IreportModel_TechSkillReportCollection_" + i + "__WeightageActual").val());

    }
    //obj.y = parseInt(techTotal) / 4;
    obj.y = parseInt(techTotal);
    obj.label = "Tech Skills Score";
    obj.legendText = "Tech Skills Score";
    arr.push(obj.y);

    //SkillsGeneral actual weightage total
    var objGen = { y: "", legendText: "", label: "" }
    var genSkillsItems = $('[id^="IreportModel_SkillGeneralReportCollection"][id$="WeightageActual"]');
    var genTotal = 0;
    for (i = 0; i < genSkillsItems.length; i++) {
        var weightage = $("#IreportModel_SkillGeneralReportCollection_" + i + "__WeightageActual").val();
        genTotal = parseInt(genTotal) + parseInt(weightage);

    }

    //objGen.y = parseInt(genTotal) / 4;
    objGen.y = parseInt(genTotal);
    objGen.label = "General Skills Score";
    objGen.legendText = "General Skills Score";
    arr.push(objGen.y);

    //Joinability actual weightage total
    var objJoin = { y: "", legendText: "", label: "" }
    var joinabilityItems = $('[id^="IreportModel_JoinabilityHRReportCollection"][id$="WeightageActual"]');
    var joinabilityTotal = 0;
    for (i = 0; i < joinabilityItems.length; i++) {

        joinabilityTotal = parseInt(joinabilityTotal) + parseInt($("#IreportModel_JoinabilityHRReportCollection_" + i + "__WeightageActual").val());

    }
    //objJoin.y = parseInt(joinabilityTotal) / 4;
    objJoin.y = parseInt(joinabilityTotal);
    objJoin.label = "Joinability Score";
    objJoin.legendText = "Joinability Score";
    arr.push(objJoin.y);

    //Selectability actual weightage total
    var objSelect = { y: "", legendText: "", label: "" }
    var selectabilityItems = $('[id^="IreportModel_SelectabilityEvaluatorReportCollection"][id$="WeightageActual"]');
    var selectabilityTotal = 0;
    for (i = 0; i < selectabilityItems.length; i++) {

        selectabilityTotal = parseInt(selectabilityTotal) + parseInt($("#IreportModel_SelectabilityEvaluatorReportCollection_" + i + "__WeightageActual").val());

    }
    //objSelect.y = parseInt(selectabilityTotal) / 4;
    objSelect.y = parseInt(selectabilityTotal);
    objSelect.label = "Selectability Score";
    objSelect.legendText = "Selectability Score";
    arr.push(objSelect.y);

    //totalScore = (techTotal + genTotal + joinabilityTotal + selectabilityTotal) / 4;
    document.getElementById("totTechScore").innerHTML = techTotal;


    am4core.ready(function () {

        // Themes begin
        am4core.useTheme(am4themes_animated);
        // Themes end

        // Create chart instance
        var chart = am4core.create("scoreChart", am4charts.XYChart);

        // Add data
        chart.data = [{
            "country": "Tech",
            "visits": arr[0]

        }, {
            "country": "General",
            "visits": arr[1]
        }, {
            "country": "Joinability",
            "visits": arr[2]
        }, {
            "country": "Selectability",
            "visits": arr[3]
        }, {

            "panelsSettings": {
                "panEventsEnabled": false
            }

        }

        ];

        // Create axes

        chart.logo.disabled = true;
        var categoryAxis = chart.xAxes.push(new am4charts.CategoryAxis());
        categoryAxis.dataFields.category = "country";
        categoryAxis.renderer.inside = true;
        categoryAxis.renderer.labels.template.valign = 0;

        categoryAxis.renderer.grid.template.location = 0;
        //categoryAxis.renderer.minGridDistance = 30;
        categoryAxis.renderer.minGridDistance = 100;
        chart.fontSize = 13;

        categoryAxis.renderer.labels.template.adapter.add("dy", function (dy, target) {
            if (target.dataItem && target.dataItem.index & 2 == 2) {
                return dy;
            }
            return dy;
        });





        var valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
        categoryAxis.renderer.grid.template.disabled = true;
        categoryAxis.renderer.labels.template.fill = am4core.color("#828080");
        categoryAxis.renderer.labels.template.paddingBottom = -17;
        valueAxis.renderer.grid.template.disabled = true;
        valueAxis.min = 0;
        //valueAxis.max = 30;
        valueAxis.max = 100;
        // Create series
        var series = chart.series.push(new am4charts.ColumnSeries());
        series.dataFields.valueY = "visits";
        series.dataFields.categoryX = "country";
        series.name = "Visits";
        series.colorField = "color";


        series.columns.template.tooltipText = "{valueY.percent}[/] ({valueY})";
        series.tooltip.label.adapter.add("text", function (text, target) {
            if (target.dataItem && target.dataItem.valueY == 0) {

                return "";
            }
            else {

                return text;
            }
        });
        series.columns.template.fillOpacity = .8;
        series.columns.template.tooltipY = 8;
        series.tooltip.background.strokeOpacity = 0
        series.tooltip.pointerOrientation = "vertical";
        series.columns.template.showTooltipOn = "always";
        series.tooltip.background.filters.clear();
        series.tooltip.getFillFromObject = false;
        series.tooltip.label.fill = am4core.color("#9a9a9a");
        series.tooltip.background.cornerRadius = 0;
        series.tooltip.label.textAlign = "middle";
        series.tooltip.background.fill = 0;
        var columnTemplate = series.columns.template;
        columnTemplate.strokeWidth = 0;
        columnTemplate.strokeOpacity = 1;
        chart.colors.list = [
            am4core.color("#07BEF3"),
            am4core.color("#00F96B"),
            am4core.color("#F64042"),
            am4core.color("#ffbc50")



        ];
        series.columns.template.events.once("inited", function (event) {
            event.target.fill = chart.colors.getIndex(event.target.dataItem.index);
        });

    });
}

function SetChartWithoutJoinabilitySelectability() {
    var arr = [];
    var obj = { y: "", legendText: "", label: "" }
    //TechSkills actual weightage total
    var techItems = $('[id^="TechSkillReportCollection"][id$="WeightageActual"]');
    var techTotal = 0;
    for (i = 0; i < techItems.length; i++) {

        techTotal = parseInt(techTotal) + parseInt($("#TechSkillReportCollection_" + i + "__WeightageActual").val());

    }
    obj.y = parseInt(techTotal);
    obj.label = "Tech Skills Score";
    obj.legendText = "Tech Skills Score";
    arr.push(obj.y);

    //SkillsGeneral actual weightage total
    var objGen = { y: "", legendText: "", label: "" }
    var genSkillsItems = $('[id^="SkillGeneralReportCollection"][id$="WeightageActual"]');
    var genTotal = 0;
    for (i = 0; i < genSkillsItems.length; i++) {
        var weightage = $("#SkillGeneralReportCollection_" + i + "__WeightageActual").val();
        genTotal = parseInt(genTotal) + parseInt(weightage);

    }

    objGen.y = parseInt(genTotal);
    objGen.label = "General Skills Score";
    objGen.legendText = "General Skills Score";
    arr.push(objGen.y);



    //totalScore = (techTotal + genTotal + joinabilityTotal + selectabilityTotal) / 4;
    //document.getElementById("totScore").innerHTML = average;
    document.getElementById("totTechScore").innerHTML = techTotal;

    am4core.ready(function () {

        // Themes begin
        am4core.useTheme(am4themes_animated);
        // Themes end

        // Create chart instance
        var chart = am4core.create("scoreChart", am4charts.XYChart);

        // Add data
        chart.data = [{
            "country": "Tech",
            "visits": arr[0]

        }, {
            "country": "General",
            "visits": arr[1]
        }];

        // Create axes
        chart.logo.disabled = true;
        var categoryAxis = chart.xAxes.push(new am4charts.CategoryAxis());
        categoryAxis.dataFields.category = "country";
        categoryAxis.renderer.inside = true;
        categoryAxis.renderer.labels.template.valign = 0;
        categoryAxis.renderer.grid.template.location = 0;
        //categoryAxis.renderer.minGridDistance = 30;
        categoryAxis.renderer.minGridDistance = 100;
        chart.fontSize = 13;
        categoryAxis.renderer.labels.template.fill = am4core.color("#828080");

        categoryAxis.renderer.labels.template.adapter.add("dy", function (dy, target) {
            if (target.dataItem && target.dataItem.index & 2 == 2) {
                return dy;
            }
            return dy;
        });
        categoryAxis.renderer.labels.template.fill = am4core.color("#828080");
        categoryAxis.renderer.labels.template.paddingBottom = -17;
        categoryAxis.renderer.grid.template.disabled = true;
        var valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
        valueAxis.renderer.grid.template.disabled = true;
        valueAxis.min = 0;
        //valueAxis.max = 30;
        valueAxis.max = 100;
        // Create series
        var series = chart.series.push(new am4charts.ColumnSeries());
        series.dataFields.valueY = "visits";
        series.dataFields.categoryX = "country";
        series.name = "Visits";
        series.colorField = "color";
        series.columns.template.tooltipText = "{valueY.percent}[/] ({valueY})";
        series.tooltip.label.adapter.add("text", function (text, target) {
            if (target.dataItem && target.dataItem.valueY == 0) {

                return "";
            }
            else {

                return text;
            }
        });
        series.columns.template.fillOpacity = .8;
        series.columns.template.tooltipY = 0;
        series.tooltip.background.strokeOpacity = 0
        series.tooltip.pointerOrientation = "vertical";
        series.columns.template.showTooltipOn = "always";
        series.tooltip.background.filters.clear();
        series.tooltip.getFillFromObject = false;

        series.tooltip.label.fill = am4core.color("#9a9a9a");
        series.tooltip.background.cornerRadius = 0;
        series.tooltip.label.textAlign = "middle";
        series.tooltip.background.fill = 0;
        var columnTemplate = series.columns.template;
        columnTemplate.strokeWidth = 0;
        columnTemplate.strokeOpacity = 1;//07BEF3
        chart.colors.list = [
            am4core.color("#07BEF3"),
            am4core.color("#00F96B")

        ];
        series.columns.template.events.once("inited", function (event) {
            event.target.fill = chart.colors.getIndex(event.target.dataItem.index);
        });

    });
}

function SetChartForVideoPageWithoutJoinabilitySelectability() {
    var arr = [];
    var obj = { y: "", legendText: "", label: "" }
    //TechSkills actual weightage total
    var techItems = $('[id^="IreportModel_TechSkillReportCollection"][id$="WeightageActual"]');
    var techTotal = 0;
    for (i = 0; i < (techItems.length / 2); i++) {

        techTotal = parseInt(techTotal) + parseInt($("#IreportModel_TechSkillReportCollection_" + i + "__WeightageActual").val());

    }
    //obj.y = parseInt(techTotal) / 4;
    obj.y = parseInt(techTotal);
    obj.label = "Tech Skills Score";
    obj.legendText = "Tech Skills Score";
    arr.push(obj.y);

    //SkillsGeneral actual weightage total
    var objGen = { y: "", legendText: "", label: "" }
    var genSkillsItems = $('[id^="IreportModel_SkillGeneralReportCollection"][id$="WeightageActual"]');
    var genTotal = 0;
    for (i = 0; i < genSkillsItems.length; i++) {
        var weightage = $("#IreportModel_SkillGeneralReportCollection_" + i + "__WeightageActual").val();
        genTotal = parseInt(genTotal) + parseInt(weightage);

    }

    //objGen.y = parseInt(genTotal) / 4;
    objGen.y = parseInt(genTotal);
    objGen.label = "General Skills Score";
    objGen.legendText = "General Skills Score";
    arr.push(objGen.y);

    //totalScore = (techTotal + genTotal + joinabilityTotal + selectabilityTotal) / 4;
    document.getElementById("totTechScore").innerHTML = techTotal;


    am4core.ready(function () {

        // Themes begin
        am4core.useTheme(am4themes_animated);
        // Themes end

        // Create chart instance
        var chart = am4core.create("scoreChart", am4charts.XYChart);

        // Add data
        chart.data = [{
            "country": "Tech",
            "visits": arr[0]

        }, {
            "country": "General",
            "visits": arr[1]
        }, {

            "panelsSettings": {
                "panEventsEnabled": false
            }

        }

        ];

        // Create axes

        chart.logo.disabled = true;
        var categoryAxis = chart.xAxes.push(new am4charts.CategoryAxis());
        categoryAxis.dataFields.category = "country";
        categoryAxis.renderer.inside = true;
        categoryAxis.renderer.labels.template.valign = 0;

        categoryAxis.renderer.grid.template.location = 0;
        //categoryAxis.renderer.minGridDistance = 30;
        categoryAxis.renderer.minGridDistance = 100;
        chart.fontSize = 13;

        categoryAxis.renderer.labels.template.adapter.add("dy", function (dy, target) {
            if (target.dataItem && target.dataItem.index & 2 == 2) {
                return dy;
            }
            return dy;
        });





        var valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
        categoryAxis.renderer.grid.template.disabled = true;
        categoryAxis.renderer.labels.template.fill = am4core.color("#828080");
        categoryAxis.renderer.labels.template.paddingBottom = -17;
        valueAxis.renderer.grid.template.disabled = true;
        valueAxis.min = 0;
        //valueAxis.max = 30;
        valueAxis.max = 100;
        // Create series
        var series = chart.series.push(new am4charts.ColumnSeries());
        series.dataFields.valueY = "visits";
        series.dataFields.categoryX = "country";
        series.name = "Visits";
        series.colorField = "color";


        series.columns.template.tooltipText = "{valueY.percent}[/] ({valueY})";
        series.tooltip.label.adapter.add("text", function (text, target) {
            if (target.dataItem && target.dataItem.valueY == 0) {

                return "";
            }
            else {

                return text;
            }
        });
        series.columns.template.fillOpacity = .8;
        series.columns.template.tooltipY = 8;
        series.tooltip.background.strokeOpacity = 0
        series.tooltip.pointerOrientation = "vertical";
        series.columns.template.showTooltipOn = "always";
        series.tooltip.background.filters.clear();
        series.tooltip.getFillFromObject = false;
        series.tooltip.label.fill = am4core.color("#9a9a9a");
        series.tooltip.background.cornerRadius = 0;
        series.tooltip.label.textAlign = "middle";
        series.tooltip.background.fill = 0;
        var columnTemplate = series.columns.template;
        columnTemplate.strokeWidth = 0;
        columnTemplate.strokeOpacity = 1;
        chart.colors.list = [
            am4core.color("#07BEF3"),
            am4core.color("#00F96B")

        ];
        series.columns.template.events.once("inited", function (event) {
            event.target.fill = chart.colors.getIndex(event.target.dataItem.index);
        });

    });
}
function SetChartMCQReport() {
    var arr = [];
    var objJoin = { y: "", legendText: "", label: "" }
    var joinabilityItems = 5;
    var joinabilityTotal = 0;
    var joinablityrow = 0;
    var fitmenttotal = 0;
    for (i = 0; i < joinabilityItems; i++) {
        if ($("#JoinabilityHRReportCollection_" + i + "__Value").val() != '') {
            var valuecheck = 0;
            joinablityrow = parseInt(joinablityrow) + 1;
            if (parseInt($("#JoinabilityHRReportCollection_" + i + "__LevelId").val()) >= parseInt($("#JoinabilityHRReportCollection_" + i + "__RequiredLevelId").val())) {
                valuecheck = 100;
            } else {
                valuecheck = 100 - ((parseInt($("#JoinabilityHRReportCollection_" + i + "__RequiredLevelId").val()) - parseInt($("#JoinabilityHRReportCollection_" + i + "__LevelId").val())) * 100 / parseInt($("#JoinabilityHRReportCollection_" + i + "__RequiredLevelId").val()));
            }
            joinabilityTotal = parseInt(joinabilityTotal) + parseInt(valuecheck);
        }
    }

    if (parseInt(joinablityrow) > 0) {
        joinabilityTotal = parseInt(joinabilityTotal) / parseInt(joinablityrow);
    }
    objJoin.y = parseInt(joinabilityTotal);
    objJoin.label = "Joinability Score";
    objJoin.legendText = "Joinability Score";
    arr.push(objJoin.y);

    var objSelect = { y: "", legendText: "", label: "" }
    var selectabilityItems = 5;
    var selectabilityTotal = 0;
    var selectabilityrow = 0;
    for (i = 0; i < selectabilityItems; i++) {
        if ($("#SelectabilityEvaluatorReportCollection_" + i + "__Value").val() != '') {
            var valuecheckselect = 0;
            selectabilityrow = parseInt(selectabilityrow) + 1;
            if (parseInt($("#SelectabilityEvaluatorReportCollection_" + i + "__LevelId").val()) >= parseInt($("#SelectabilityEvaluatorReportCollection_" + i + "__RequiredLevelId").val())) {
                valuecheckselect = 100;
            } else {
                valuecheckselect = 100 - ((parseInt($("#SelectabilityEvaluatorReportCollection_" + i + "__RequiredLevelId").val()) - parseInt($("#SelectabilityEvaluatorReportCollection_" + i + "__LevelId").val())) * 100 / parseInt($("#SelectabilityEvaluatorReportCollection_" + i + "__RequiredLevelId").val()));
            }
            selectabilityTotal = parseInt(selectabilityTotal) + parseInt(valuecheckselect);
        }
    }

    if (parseInt(selectabilityrow) > 0) {
        selectabilityTotal = parseInt(selectabilityTotal) / parseInt(selectabilityrow);
    }
    objSelect.y = parseInt(selectabilityTotal);
    objSelect.label = "Selectability Score";
    objSelect.legendText = "Selectability Score";
    arr.push(objSelect.y);

    fitmenttotal = (parseInt(selectabilityTotal) + parseInt(joinabilityTotal)) / 2;
    document.getElementById("totFitMentScore").innerHTML = fitmenttotal;

    am4core.ready(function () {
        am4core.useTheme(am4themes_animated);
        var chart = am4core.create("scoreChart", am4charts.XYChart);
        chart.data = [{
            "country": "Joinability",
            "visits": arr[0]
        }, {
            "country": "Selectability",
            "visits": arr[1]
        }];

        chart.logo.disabled = true;
        var categoryAxis = chart.xAxes.push(new am4charts.CategoryAxis());
        categoryAxis.dataFields.category = "country";
        categoryAxis.renderer.inside = true;
        categoryAxis.renderer.labels.template.valign = 0;
        categoryAxis.renderer.grid.template.location = 0;
        //categoryAxis.renderer.minGridDistance = 30;
        categoryAxis.renderer.minGridDistance = 100;
        chart.fontSize = 13;
        categoryAxis.renderer.labels.template.fill = am4core.color("#828080");

        categoryAxis.renderer.labels.template.adapter.add("dy", function (dy, target) {
            if (target.dataItem && target.dataItem.index & 2 == 2) {
                return dy;
            }
            return dy;
        });
        categoryAxis.renderer.labels.template.fill = am4core.color("#828080");
        categoryAxis.renderer.labels.template.paddingBottom = -17;
        categoryAxis.renderer.grid.template.disabled = true;
        var valueAxis = chart.yAxes.push(new am4charts.ValueAxis());
        valueAxis.renderer.grid.template.disabled = true;
        valueAxis.min = 0;
        //valueAxis.max = 30;
        valueAxis.max = 100;
        // Create series
        var series = chart.series.push(new am4charts.ColumnSeries());
        series.dataFields.valueY = "visits";
        series.dataFields.categoryX = "country";
        series.name = "Visits";
        series.colorField = "color";
        series.columns.template.tooltipText = "{valueY.percent}[/] ({valueY})";
        series.tooltip.label.adapter.add("text", function (text, target) {
            if (target.dataItem && target.dataItem.valueY == 0) {
                return "";
            }
            else {
                return text;
            }
        });
        series.columns.template.fillOpacity = .8;
        series.columns.template.tooltipY = 0;
        series.tooltip.background.strokeOpacity = 0
        series.tooltip.pointerOrientation = "vertical";
        series.columns.template.showTooltipOn = "always";
        series.tooltip.background.filters.clear();
        series.tooltip.getFillFromObject = false;

        series.tooltip.label.fill = am4core.color("#9a9a9a");
        series.tooltip.background.cornerRadius = 0;
        series.tooltip.label.textAlign = "middle";
        series.tooltip.background.fill = 0;
        var columnTemplate = series.columns.template;
        columnTemplate.strokeWidth = 0;
        columnTemplate.strokeOpacity = 1;//07BEF3
        chart.colors.list = [
            am4core.color("#07BEF3"),
            am4core.color("#00F96B"),
            am4core.color("#F64042"),
            am4core.color("#ffbc50")
        ];
        series.columns.template.events.once("inited", function (event) {
            event.target.fill = chart.colors.getIndex(event.target.dataItem.index);
        });
    });
}

//function SetTechReportChart(techCollectionIdStartsWith) {

//    var idSelector = '[id^=' + '"' + techCollectionIdStartsWith + '"' + '][id$="WeightageActual"]';
//    //var techItems = $('[id^="TechSkillReportCollection"][id$="WeightageActual"]');
//    var techItems = $(idSelector);
//    var techSkillGraphObjectCollection = [];
//    var totalTechSkillScore = 0;
//    if (techItems != null) {
//        var techItemsLength = 0;
//        if (techCollectionIdStartsWith == 'IreportModel_TechSkillReportCollection') {
//            techItemsLength = techItems.length / 2;
//        }
//        else {
//            techItemsLength = techItems.length;
//        }
//        for (i = 0; i < techItemsLength; i++) {

//            //var weightage = document.getElementById("TechSkillReportCollection_" + i + "__WeightageActual").value;
//            var weightage = document.getElementById(techCollectionIdStartsWith + "_" + i + "__WeightageActual").value;
//            if (weightage != null && weightage != '') {
//                totalTechSkillScore = totalTechSkillScore + parseFloat(weightage);
//                weightage = parseFloat(weightage).toFixed(2);
//            }
//            //var skill = document.getElementById("TechSkillReportCollection_" + i + "__SkillName").value;
//            var skill = document.getElementById(techCollectionIdStartsWith + "_" + i + "__SkillName").value;
//            var trimmedSkill = '';
//            if (skill != null && skill.length > 15) {
//                trimmedSkill = skill.substring(0, 14) + '...';
//            }
//            else {
//                trimmedSkill = skill;
//            }
//            var techSkillGraphObject = { y: weightage, label: skill, trimmedLabel: trimmedSkill };
//            techSkillGraphObjectCollection.push(techSkillGraphObject);
//        }

//        var chart = new CanvasJS.Chart("scoreChart", {
//            theme: "light2",
//            width: 375,//in pixels
//            height: 200,//in pixels
//            animationEnabled: true,
//            title: null,
//            data: [{
//                type: "pie",
//                startAngle: 60,
//                innerRadius: 55,
//                indexLabelFontSize: 15,
//                indexLabelFontColor: "#9a9a9a",
//                indexLabel: "{trimmedLabel}  {y}",
//                toolTipContent: "<b>{label} ({y}) </b> ",

//                dataPoints: techSkillGraphObjectCollection
//            }]
//        });
//        chart.render();

//        document.getElementById("totTechScore").innerHTML = (parseFloat(totalTechSkillScore).toFixed(2)).toString() + '%';

//    }



//}
function SetTechReportChart(techCollectionIdStartsWith) {

    var idSelector = '[id^=' + '"' + techCollectionIdStartsWith + '"' + '][id$="WeightageActual"]';
    var techItems = $(idSelector);
    var totalTechSkillScore = 0;

    const labels = [];

    const requiredWeightageData = [];
    const requiredWeightageBackgroundColour = [];
    const requiredWeightageBorderColour = [];

    const actualWeightageData = [];
    const actualWeightageBackgroundColour = [];
    const actualWeightageBorderColour = [];

    if (techItems != null) {
        var techItemsLength = 0;
        if (techCollectionIdStartsWith == 'IreportModel_TechSkillReportCollection') {
            techItemsLength = techItems.length / 2;
        }
        else {
            techItemsLength = techItems.length;
        }
        for (i = 0; i < techItemsLength; i++) {

            var skill = document.getElementById(techCollectionIdStartsWith + "_" + i + "__SkillName").value;
            var formattedSkill = formatLabel(skill, 10);
            labels.push(formattedSkill);

            var requiredWeightage = document.getElementById(techCollectionIdStartsWith + "_" + i + "__WeightageRequired").value;
            requiredWeightageData.push(requiredWeightage);
            requiredWeightageBackgroundColour.push('rgba(255, 26, 104, 0.2)');
            requiredWeightageBorderColour.push('rgba(255, 26, 104, 1)');


            var actualWeightage = document.getElementById(techCollectionIdStartsWith + "_" + i + "__WeightageActual").value;
            actualWeightageData.push(actualWeightage);
            actualWeightageBackgroundColour.push('rgba(34, 139, 34, 0.2)');
            actualWeightageBorderColour.push('rgba(34, 139, 34, 1)');

            if (actualWeightage != null && actualWeightage != '') {
                totalTechSkillScore = totalTechSkillScore + parseFloat(actualWeightage);
            }

        }

        const data = {
            labels: labels,
            datasets: [{
                label: 'Required',
                data: requiredWeightageData,
                backgroundColor: requiredWeightageBackgroundColour,
                borderColor: requiredWeightageBorderColour,
                borderWidth: 1
            },
            {
                label: 'Actual',
                data: actualWeightageData,
                backgroundColor: actualWeightageBackgroundColour,
                borderColor: actualWeightageBorderColour,
                borderWidth: 1
            }]
        };

        const config = {
            type: 'bar',
            data: data,
            options: {
                scales: {
                    y: {
                        beginAtZero: true,
                        min: 0,
                        //stepSize: 5,
                        ticks: {
                            stepSize: 5
                        }
                    },
                    xAxes: [{
                        ticks: {
                            fontSize: 12
                        }
                    }]
                },
                responsive: false
            },
        };


        const scoreChart = document.getElementById("scoreChart");
        if (scoreChart != null) {
            let width = scoreChart.style.width;
            if (width == null || width == "") {
                width = scoreChart.getAttribute("width");
            }
            let height = scoreChart.style.height;
            if (height == null || height == "") {
                height = scoreChart.getAttribute("height");
            }
            scoreChart.remove();


            const canvasElement = document.createElement("canvas");
            canvasElement.setAttribute("id", "scoreChart");
            canvasElement.setAttribute("height", height);
            canvasElement.setAttribute("width", width);
            document.getElementById("canvasDiv").appendChild(canvasElement);


            var canvas = document.getElementById("scoreChart");
            var ctx = canvas.getContext('2d');

            const myChart = new Chart(
                ctx,
                config
            );

            document.getElementById("totTechScore").innerHTML = (parseFloat(totalTechSkillScore).toFixed(2)).toString() + '%';

        }

    }



}

function formatLabel(str, maxwidth) {
    var sections = [];
    var words = str.split(" ");
    var temp = "";

    words.forEach(function (item, index) {
        if (temp.length > 0) {
            var concat = temp + ' ' + item;

            if (concat.length > maxwidth) {
                sections.push(temp);
                temp = "";
            }
            else {
                if (index == (words.length - 1)) {
                    sections.push(concat);
                    return;
                }
                else {
                    temp = concat;
                    return;
                }
            }
        }

        if (index == (words.length - 1)) {
            sections.push(item);
            return;
        }

        if (item.length < maxwidth) {
            temp = item;
        }
        else {
            sections.push(item);
        }

    });

    return sections;
}

function CalculateOverAllScore(requiredLevel, actualLevel, requiredWeightage) {
    var actualWeightage = 0;
    if (actualLevel != null && actualLevel != '') {
        if (requiredLevel <= actualLevel) {
            actualWeightage = requiredWeightage;

        }
        else {
            actualWeightage = ((5 - (requiredLevel - actualLevel)) / 5) * (requiredWeightage);
        }
    }
    return actualWeightage;
}