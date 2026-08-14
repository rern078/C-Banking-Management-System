(function () {
  "use strict";

  var root = document.getElementById("graphCalc");
  if (!root) return;

  var canvas = root.querySelector("#gcCanvas");
  var ctx = canvas.getContext("2d");
  var exprInput = root.querySelector("#gcExpr");
  var xminInput = root.querySelector("#gcXmin");
  var xmaxInput = root.querySelector("#gcXmax");
  var yminInput = root.querySelector("#gcYmin");
  var ymaxInput = root.querySelector("#gcYmax");
  var statusEl = root.querySelector("#gcStatus");
  var plotBtn = root.querySelector("#gcPlot");
  var clearBtn = root.querySelector("#gcClear");
  var cursorEl = root.querySelector("#gcCursor");

  function sizeCanvas() {
    var rect = canvas.parentElement.getBoundingClientRect();
    var dpr = window.devicePixelRatio || 1;
    var w = Math.max(320, Math.floor(rect.width));
    var h = Math.max(260, Math.floor(rect.height || 320));
    canvas.width = Math.floor(w * dpr);
    canvas.height = Math.floor(h * dpr);
    canvas.style.width = w + "px";
    canvas.style.height = h + "px";
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    return { w: w, h: h };
  }

  function sanitize(expr) {
    var cleaned = String(expr || "")
      .replace(/\^/g, "**")
      .replace(/π/gi, "PI")
      .replace(/\bpi\b/gi, "PI")
      .replace(/\be\b/g, "E")
      .replace(/\s+/g, "");

    if (!cleaned) throw new Error("Enter an expression in x.");
    if (!/^[0-9xX+\-*/().,EPabcdefghijklmnopqrstuvwxyz]+$/i.test(cleaned)) {
      throw new Error("Only numbers, x, and math functions are allowed.");
    }
    if (/[;=`{}\[\]\\]|import|window|document|Function|eval|this/i.test(cleaned)) {
      throw new Error("Invalid characters in expression.");
    }

    cleaned = cleaned
      .replace(/\bsin\b/gi, "Math.sin")
      .replace(/\bcos\b/gi, "Math.cos")
      .replace(/\btan\b/gi, "Math.tan")
      .replace(/\basin\b/gi, "Math.asin")
      .replace(/\bacos\b/gi, "Math.acos")
      .replace(/\batan\b/gi, "Math.atan")
      .replace(/\bsqrt\b/gi, "Math.sqrt")
      .replace(/\babs\b/gi, "Math.abs")
      .replace(/\blog\b/gi, "Math.log10")
      .replace(/\bln\b/gi, "Math.log")
      .replace(/\bexp\b/gi, "Math.exp")
      .replace(/\bfloor\b/gi, "Math.floor")
      .replace(/\bceil\b/gi, "Math.ceil")
      .replace(/\bround\b/gi, "Math.round")
      .replace(/\bmin\b/gi, "Math.min")
      .replace(/\bmax\b/gi, "Math.max")
      .replace(/\bPI\b/g, "Math.PI")
      .replace(/\bE\b/g, "Math.E")
      .replace(/\bx\b/gi, "x");

    return cleaned;
  }

  function compile(expr) {
    var body = sanitize(expr);
    // eslint-disable-next-line no-new-func
    var fn = new Function("x", "return (" + body + ");");
    fn(0);
    return fn;
  }

  function num(el, fallback) {
    var v = parseFloat(el.value);
    return Number.isFinite(v) ? v : fallback;
  }

  function mapX(x, xmin, xmax, w) {
    return ((x - xmin) / (xmax - xmin)) * w;
  }

  function mapY(y, ymin, ymax, h) {
    return h - ((y - ymin) / (ymax - ymin)) * h;
  }

  function drawAxes(w, h, xmin, xmax, ymin, ymax) {
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = "#f7fbfa";
    ctx.fillRect(0, 0, w, h);

    ctx.strokeStyle = "#d7e5e1";
    ctx.lineWidth = 1;
    var xSteps = 10;
    var ySteps = 8;
    for (var i = 0; i <= xSteps; i++) {
      var gx = (w / xSteps) * i;
      ctx.beginPath();
      ctx.moveTo(gx, 0);
      ctx.lineTo(gx, h);
      ctx.stroke();
    }
    for (var j = 0; j <= ySteps; j++) {
      var gy = (h / ySteps) * j;
      ctx.beginPath();
      ctx.moveTo(0, gy);
      ctx.lineTo(w, gy);
      ctx.stroke();
    }

    var ox = mapX(0, xmin, xmax, w);
    var oy = mapY(0, ymin, ymax, h);
    ctx.strokeStyle = "#7a9691";
    ctx.lineWidth = 1.5;
    if (ox >= 0 && ox <= w) {
      ctx.beginPath();
      ctx.moveTo(ox, 0);
      ctx.lineTo(ox, h);
      ctx.stroke();
    }
    if (oy >= 0 && oy <= h) {
      ctx.beginPath();
      ctx.moveTo(0, oy);
      ctx.lineTo(w, oy);
      ctx.stroke();
    }

    ctx.fillStyle = "#5a7276";
    ctx.font = "11px Manrope, sans-serif";
    ctx.fillText(xmin.toFixed(1), 6, oy > 18 ? oy - 6 : oy + 14);
    ctx.fillText(xmax.toFixed(1), w - 34, oy > 18 ? oy - 6 : oy + 14);
    ctx.fillText(ymin.toFixed(1), ox < w - 40 ? ox + 6 : ox - 36, h - 8);
    ctx.fillText(ymax.toFixed(1), ox < w - 40 ? ox + 6 : ox - 36, 14);
  }

  function plot() {
    var size = sizeCanvas();
    var w = size.w;
    var h = size.h;
    var xmin = num(xminInput, -10);
    var xmax = num(xmaxInput, 10);
    var ymin = num(yminInput, -10);
    var ymax = num(ymaxInput, 10);

    if (xmin >= xmax || ymin >= ymax) {
      statusEl.textContent = "Check your axis ranges.";
      statusEl.className = "gc-status bad";
      return;
    }

    drawAxes(w, h, xmin, xmax, ymin, ymax);

    var fn;
    try {
      fn = compile(exprInput.value);
    } catch (err) {
      statusEl.textContent = err.message || "Could not parse expression.";
      statusEl.className = "gc-status bad";
      return;
    }

    ctx.strokeStyle = "#0a524c";
    ctx.lineWidth = 2.2;
    ctx.beginPath();
    var started = false;
    var samples = Math.max(400, w * 2);
    var drawn = 0;

    for (var i = 0; i <= samples; i++) {
      var x = xmin + ((xmax - xmin) * i) / samples;
      var y;
      try {
        y = fn(x);
      } catch (_) {
        started = false;
        continue;
      }
      if (!Number.isFinite(y)) {
        started = false;
        continue;
      }
      var px = mapX(x, xmin, xmax, w);
      var py = mapY(y, ymin, ymax, h);
      if (py < -50 || py > h + 50) {
        started = false;
        continue;
      }
      if (!started) {
        ctx.moveTo(px, py);
        started = true;
      } else {
        ctx.lineTo(px, py);
      }
      drawn++;
    }
    ctx.stroke();

    if (drawn < 2) {
      statusEl.textContent = "No points to plot in this range.";
      statusEl.className = "gc-status bad";
    } else {
      statusEl.textContent = "Plotted y = " + exprInput.value.trim();
      statusEl.className = "gc-status ok";
    }
  }

  function clearGraph() {
    exprInput.value = "";
    xminInput.value = "-10";
    xmaxInput.value = "10";
    yminInput.value = "-10";
    ymaxInput.value = "10";
    var size = sizeCanvas();
    drawAxes(size.w, size.h, -10, 10, -10, 10);
    statusEl.textContent = "Ready. Enter an expression in x.";
    statusEl.className = "gc-status";
    if (cursorEl) cursorEl.textContent = "";
  }

  root.querySelectorAll("[data-expr]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      exprInput.value = btn.getAttribute("data-expr") || "";
      if (btn.dataset.xmin) xminInput.value = btn.dataset.xmin;
      if (btn.dataset.xmax) xmaxInput.value = btn.dataset.xmax;
      if (btn.dataset.ymin) yminInput.value = btn.dataset.ymin;
      if (btn.dataset.ymax) ymaxInput.value = btn.dataset.ymax;
      plot();
    });
  });

  plotBtn.addEventListener("click", plot);
  clearBtn.addEventListener("click", clearGraph);
  exprInput.addEventListener("keydown", function (e) {
    if (e.key === "Enter") {
      e.preventDefault();
      plot();
    }
  });

  canvas.addEventListener("mousemove", function (e) {
    var rect = canvas.getBoundingClientRect();
    var xmin = num(xminInput, -10);
    var xmax = num(xmaxInput, 10);
    var ymin = num(yminInput, -10);
    var ymax = num(ymaxInput, 10);
    var x = xmin + ((e.clientX - rect.left) / rect.width) * (xmax - xmin);
    var y = ymax - ((e.clientY - rect.top) / rect.height) * (ymax - ymin);
    if (cursorEl) cursorEl.textContent = "x = " + x.toFixed(2) + " · y = " + y.toFixed(2);
  });

  window.addEventListener("resize", function () {
    plot();
  });

  clearGraph();
  exprInput.value = "x^2";
  plot();
})();
