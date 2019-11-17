const marked = require('marked')
const _ = require('lodash')

module.exports = grunt => {
  grunt.registerTask('compile-docs', function() {
    const metadata = grunt.file.readJSON('./src/docs/metadata.json')

    for (const doc of metadata) {
      const mdPath = `../Docs/${doc.slug}.md`
      const md = grunt.file.read(mdPath)
      doc.html = marked(md)
      grunt.log.writeln(`File "${mdPath}" compiled`)
    }

    const docsTemplate = grunt.file.read('./src/docs/layout.jst')
    const docsHtml = _.template(docsTemplate)({ docs: metadata })

    const siteTemplate = grunt.file.read('./src/layout.jst')
    const siteHtml = _.template(siteTemplate)({
      content: docsHtml,
      title: 'Imgbot - Docs'
    })

    grunt.file.write('./dist/docs/index.html', siteHtml)
  })
}
