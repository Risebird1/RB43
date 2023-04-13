var counter = 0;
(function () {
    // Slide In Panel - by CodyHouse.co
	var panelTriggers = document.getElementsByClassName('js-cd-panel-trigger');
	if( panelTriggers.length > 0 ) {
		for(var i = 0; i < panelTriggers.length; i++) {
			(function(i){
				var panelClass = 'js-cd-panel-'+panelTriggers[i].getAttribute('data-panel'),
					panel = document.getElementsByClassName(panelClass)[0];
				// open panel when clicking on trigger btn
				var placeholderElement = $('#sidePanel_clientFeed_placeholder');
				panelTriggers[i].addEventListener('click', function (event) {
					counter = counter + 1;
					event.preventDefault();
					var url = $(this).data('url');
					if (counter == 1) {
						if (sessionStorage.getItem("DisplayPanel") == null || sessionStorage.getItem("DisplayPanel") === undefined || sessionStorage.getItem("DisplayPanel") == '') {
							addClass(panel, 'cd-panel--is-visible');
							$.get(url).done(function (data) {
								placeholderElement.html('');
								placeholderElement.html(data);
							});

							sessionStorage.setItem("DisplayPanel", false);
						}
						

					}
					else {
						if (sessionStorage.getItem("DisplayPanel") != false) {
							var dates = document.getElementsByClassName('date 1');
							var dateValue;
							var diffOffset = null;

							if (dates != null && dates.length > 0) {
								dateValue = dates[0].dataset.val;
							}
							else {
								var date = new Date();
								dateValue = date.toJSON();
								diffOffset = date.getTimezoneOffset();
							}

							$.get(url, { postWhichDate: dateValue, diffOffset: diffOffset }).done(function (data) {
								var pointerType = event.pointerType;
								if (pointerType == "mouse") {
									addClass(panel, 'cd-panel--is-visible');
								}
								else {
									if (data != null && data != '') {
										addClass(panel, 'cd-panel--is-visible');
									}
								}
								if (data != null && data != '') {
									placeholderElement.prepend(data);
								}

							});

						}
						
                    }
					
				});
				//close panel when clicking on 'x' or outside the panel
				panel.addEventListener('click', function(event){
					if( hasClass(event.target, 'js-cd-close') || hasClass(event.target, panelClass)) {
						event.preventDefault();
						removeClass(panel, 'cd-panel--is-visible');
					}
				});
			})(i);
		}
	}
	
	//class manipulations - needed if classList is not supported
	//https://jaketrent.com/post/addremove-classes-raw-javascript/
	function hasClass(el, className) {
	  	if (el.classList) return el.classList.contains(className);
	  	else return !!el.className.match(new RegExp('(\\s|^)' + className + '(\\s|$)'));
	}
	function addClass(el, className) {
	 	if (el.classList) el.classList.add(className);
	 	else if (!hasClass(el, className)) el.className += " " + className;
	}
	function removeClass(el, className) {
	  	if (el.classList) el.classList.remove(className);
	  	else if (hasClass(el, className)) {
	    	var reg = new RegExp('(\\s|^)' + className + '(\\s|$)');
	    	el.className=el.className.replace(reg, ' ');
	  	}
	}
})();