const fs = require('fs');
const { exec } = require('child_process');
const testFolder = './src/assets/i18n/';

fs.readdir(testFolder, (err, files) => {
  let allFiles = files.map(file => testFolder + file).join(' ');
  console.log(allFiles);
  exec('comparejson -e ' + allFiles, function (err, stdout, stderr) {
    if (err) {
      console.error("\n" + stderr);
      process.exit(1);
    }
    console.log(stdout);
  });
});
