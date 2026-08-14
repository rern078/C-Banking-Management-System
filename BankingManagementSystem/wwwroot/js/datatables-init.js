(() => {
  if (typeof jQuery === "undefined" || !jQuery.fn.DataTable) return;

  jQuery(".data-table").each(function () {
    if (jQuery.fn.dataTable.isDataTable(this)) return;

    jQuery(this).DataTable({
      pageLength: 10,
      lengthMenu: [10, 25, 50, 100],
      order: [],
      language: {
        search: "Search:",
        lengthMenu: "Show _MENU_ rows",
        info: "Showing _START_ to _END_ of _TOTAL_",
        infoEmpty: "No rows to show",
        zeroRecords: "No matching rows",
        paginate: {
          next: "Next",
          previous: "Prev"
        }
      }
    });
  });
})();
