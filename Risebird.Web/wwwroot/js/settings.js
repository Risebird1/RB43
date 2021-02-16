var site_settings = '<div class="ts-button">'
        +'<span class="fa fa-cogs"></span>'
    +'</div>'
    +'<div class="ts-body">'
    + '<div class="ts-title">Color Themes</div>'
    + '<div class="ts-title-label clearfix"><span class="pull-left">Light </span> <span class="pull-right">Dark </span></div>'
        +'<div class="ts-themes">'
    +'<a href="#"  data-theme="../../Newassets/css/blue-theme.css?v=546787"><img  src="../../Newassets/img/default.jpg"/></a>'            
    +'<a href="#" data-theme="../../Newassets/css/grey-theme.css?v=546787"><img  src="../../Newassets/img/grey.png"/></a>'
    +'<a href="#" data-theme="../../Newassets/css/orange-theme.css?v=546787"><img src="../../Newassets/img/orange.png"/></a>'                        
           
    +'<a href="#" data-theme="../../Newassets/css/dark-theme.css?v=546787"><img src="../../Newassets/img/dark.png"/></a>'
        +'</div>'
	
        
        
    + '</div>';



 
    
var settings_block = document.createElement('div');
    settings_block.className = "theme-settings";
    settings_block.innerHTML = site_settings;
    document.body.appendChild(settings_block);

$(document).ready(function(){

    
    /* Change Theme */
    $(".ts-themes a").click(function(){
        $(".ts-themes a").removeClass("active");
      //  $(this).addClass("active");
        $("#theme").attr("href", $(this).data("theme"));
        /*implement the ajax call*/
        
        $.ajax({
            type: "POST",
            url: "/Chat/Savetheme",
            data: { theme: $(this).data("theme"),  },
            success: function (data) {
               
             
            }
        });

        return false;
    });



    /* END Change Theme */
    
    /* Open/Hide Settings */
    $(".ts-button").on("click", function () {
       
        $(".theme-settings").toggleClass("active");

      
    });


    $(".ts-themes a").on("click", function () {
        $(".theme-settings").removeClass("active");
    })

    /* End open/hide settings */
});

