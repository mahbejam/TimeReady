// The application runs with zone-based change detection, so the tests need the
// same runtime. Angular's unit-test builder does not load Zone.js by itself.
import 'zone.js';
import 'zone.js/testing';
