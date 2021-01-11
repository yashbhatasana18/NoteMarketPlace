/* =======================================================================
                Navigation
======================================================================= */
$(function () {
    // on page load
    showHideNav();

    $(window).scroll(function () {
        showHideNav();
    });

    function showHideNav() {
        if ($(window).scrollTop() >= 0) {
            // show white navigation
            $("nav").addClass("white-nav-top");
            // logo
            $(".navbar-brand img").attr("src", "../images/logo.png");
        } else {
            // hide white navigation
            $("nav").removeClass("white-nav-top");
            $(".navbar-brand img").attr("src", "../images/top-logo.png");
        }
    }
});

/* =======================================================================
                Mobile Navigation
======================================================================= */
$(function () {
    // show
    $("#mobile-nav-open-btn").click(function () {
        $("#mobile-nav").css("height", "100%");
        $("#mobile-nav-open-btn").css("display", "none");
    });
    // hide
    $("#mobile-nav-close-btn").click(function () {
        $("#mobile-nav").css("height", "0%");
        $("#mobile-nav-open-btn").css("display", "");
    });
});
