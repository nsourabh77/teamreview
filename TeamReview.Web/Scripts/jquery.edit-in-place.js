(function ($) {
	// edit in place script: http://www.egstudio.biz/tiny-inline-edit-plugin-for-jquery/
	$.fn.inlineEdit = function (replaceWith, connectWith) {

		$(this).hover(function () {
			$(this).addClass('hover');
		}, function () {
			$(this).removeClass('hover');
		});

		$(this).click(function () {

			var elem = $(this);

			elem.hide();
			elem.after(replaceWith);

			replaceWith.focus();
			replaceWith.val(connectWith.val());

			replaceWith.blur(function () {

				if ($(this).val() != "") {
					connectWith.val($(this).val()).change();
					elem.text($(this).val());
				}

				$(this).remove();
				elem.show();
			});
		});
	};
})(jQuery);