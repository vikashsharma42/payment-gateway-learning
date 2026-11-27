// Re-opens the create-user popup after a failed submit. The page sets data-show on this script tag.
(function () {
  var script = document.currentScript;
  if (!script || script.dataset.show !== 'true') return;

  document.addEventListener('DOMContentLoaded', function () {
    var popup = document.getElementById('createUserModal');
    if (popup && window.bootstrap) new bootstrap.Modal(popup).show();
  });
})();
