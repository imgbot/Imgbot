module.exports = function(grunt) {
  grunt.loadNpmTasks('grunt-contrib-clean')
  grunt.loadNpmTasks('grunt-markdown')
  grunt.loadNpmTasks('grunt-contrib-less')
  grunt.loadNpmTasks('grunt-contrib-cssmin')
  grunt.loadNpmTasks('grunt-contrib-copy')
  grunt.loadNpmTasks('grunt-devserver')
  grunt.loadNpmTasks('grunt-contrib-watch')
  grunt.loadTasks('./tasks')

  grunt.initConfig({
    clean: ['./dist'],
    markdown: {
      all: {
        files: [
          {
            cwd: 'src',
            expand: true,
            src: '**/*.md',
            dest: 'dist/.',
            ext: '.html'
          },
          {
            cwd: 'src',
            expand: true,
            src: '**/*.html',
            dest: 'dist/.',
            ext: '.html'
          }
        ],
        options: {
          template: 'src/layout.jst',
          headerIds: false,
          gfm: true
        }
      }
    },
    less: {
      all: {
        files: {
          'dist/css/site.css': 'src/css/site.less'
        }
      }
    },
    cssmin: {
      target: {
        files: {
          'dist/css/site.min.css': 'dist/css/site.css'
        }
      }
    },
    copy: {
      all: {
        files: [
          {
            expand: true,
            cwd: 'node_modules',
            src: ['bootstrap/dist/**', 'jquery/dist/**'],
            dest: 'dist/lib/'
          },
          {
            expand: true,
            cwd: 'src',
            src: ['favicon.ico', 'images/**'],
            dest: 'dist'
          }
        ]
      }
    },
    watch: {
      scripts: {
        files: ['src/**/*.*'],
        tasks: ['gen'],
        options: {
          spawn: false
        }
      }
    },
    devserver: {
      dist: {
        options: {
          base: 'dist'
        }
      }
    }
  })

  grunt.registerTask('gen', [
    'clean',
    'compile-docs',
    'markdown:all',
    'less:all',
    'cssmin',
    'copy'
  ])
}
