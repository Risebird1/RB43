var MyApp = angular.module("MyApp", ['ngMessages', 'angularUtils.directives.dirPagination', 'isteven-multi-select', 'infinite-scroll']);
MyApp.directive('loading', function () {
    return {
        restrict: 'E',
        replace: true,
        template: "<div class='loader-me' style='display:none;width: 100%; height: 500px; background-color: #fff; position: static; z-index: 100; opacity: 1; opacity: 0.5;filter: alpha(opacity = 50); background-image: url(../img/loaders/default.gif); background-repeat: no-repeat;    background-size: 160px 80px; background-position: center center;left:0;top:0;margin-top: -20px;animation-duration:initial;animation-fill-mode:none;'></div>",
        link: function (scope, element, attr) {
            scope.$watch('loading', function (val) {
                if (val)
                    $(element).show();
                else
                    $(element).hide();
            });
        }
    }
})

MyApp.config(['$httpProvider', function ($httpProvider) {
    $httpProvider.defaults.headers.common["X-Requested-With"] = 'XMLHttpRequest';
    var values = $('input[name="__RequestVerificationToken"]').map(function () {
        return this.value
    }).get()
    $httpProvider.defaults.headers.common["RequestVerificationToken"] = values[0];
}]); 